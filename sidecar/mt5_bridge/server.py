"""ZeroMQ server for the MT5 sidecar."""

from __future__ import annotations

import asyncio
import logging
from datetime import datetime, timezone
from typing import Any

import zmq
import zmq.asyncio

from models import (
    AccountState,
    CloseParams,
    Command,
    ConnectParams,
    GetHistoryParams,
    GetPositionsParams,
    ModifyParams,
    PlaceOrderParams,
    Request,
    Response,
)
from mt5_wrapper import Mt5Error, Mt5Wrapper

logger = logging.getLogger(__name__)


class Mt5Server:
    """ZeroMQ server exposing MT5 operations.

    REP socket: synchronous command/response
    PUB socket: periodic account state stream
    """

    def __init__(
        self,
        rep_port: int = 5556,
        pub_port: int = 5557,
        state_interval: float = 1.0,
    ) -> None:
        self._rep_port = rep_port
        self._pub_port = pub_port
        self._state_interval = state_interval
        self._wrapper = Mt5Wrapper()
        self._ctx: zmq.asyncio.Context | None = None
        self._running = False

    async def run(self) -> None:
        """Start the server and block until stopped."""
        self._ctx = zmq.asyncio.Context()
        self._running = True

        rep_socket = self._ctx.socket(zmq.REP)
        rep_socket.bind(f"tcp://127.0.0.1:{self._rep_port}")

        pub_socket = self._ctx.socket(zmq.PUB)
        pub_socket.bind(f"tcp://127.0.0.1:{self._pub_port}")

        # Signal to C# process manager that we're ready
        print(f"READY rep={self._rep_port} pub={self._pub_port}", flush=True)
        logger.info("MT5 sidecar ready on REP=%d PUB=%d", self._rep_port, self._pub_port)

        try:
            await asyncio.gather(
                self._rep_loop(rep_socket),
                self._pub_loop(pub_socket),
            )
        finally:
            rep_socket.close()
            pub_socket.close()
            self._ctx.term()

    async def stop(self) -> None:
        """Signal the server to stop."""
        self._running = False

    async def _rep_loop(self, socket: zmq.asyncio.Socket) -> None:
        """Handle REQ/REP command messages."""
        while self._running:
            try:
                # Use poll to allow checking _running flag
                if await socket.poll(timeout=500) == 0:
                    continue

                raw = await socket.recv_string()
                logger.debug("REQ: %s", raw)

                response = await self._dispatch(raw)
                reply = response.model_dump_json()
                logger.debug("REP: %s", reply)

                await socket.send_string(reply)
            except zmq.ZMQError as e:
                if self._running:
                    logger.error("ZMQ error in REP loop: %s", e)
                break
            except asyncio.CancelledError:
                break

    async def _pub_loop(self, socket: zmq.asyncio.Socket) -> None:
        """Publish account state at regular intervals."""
        while self._running:
            try:
                await asyncio.sleep(self._state_interval)
                if not self._wrapper.is_connected:
                    continue

                positions = await self._wrapper.get_positions()
                balance = await self._wrapper.get_balance()
                state = AccountState(
                    positions=positions,
                    balance=balance,
                    timestamp=datetime.now(tz=timezone.utc),
                )
                msg = f"account_state {state.model_dump_json()}"
                await socket.send_string(msg)
            except asyncio.CancelledError:
                break
            except Exception:
                logger.exception("Error in PUB loop")

    async def _dispatch(self, raw: str) -> Response:
        """Parse and dispatch a command."""
        try:
            request = Request.model_validate_json(raw)
        except Exception as e:
            return Response(success=False, error=f"Invalid request: {e}")

        handlers: dict[Command, Any] = {
            Command.CONNECT: self._handle_connect,
            Command.DISCONNECT: self._handle_disconnect,
            Command.PLACE_ORDER: self._handle_place_order,
            Command.MODIFY: self._handle_modify,
            Command.CLOSE: self._handle_close,
            Command.GET_POSITIONS: self._handle_get_positions,
            Command.GET_BALANCE: self._handle_get_balance,
            Command.GET_HISTORY: self._handle_get_history,
            Command.PING: self._handle_ping,
        }

        handler = handlers.get(request.command)
        if handler is None:
            return Response(success=False, error=f"Unknown command: {request.command}")

        try:
            return await handler(request.params)
        except Mt5Error as e:
            logger.warning("MT5 error: %s", e)
            return Response(success=False, error=str(e))
        except Exception as e:
            logger.exception("Unhandled error in command %s", request.command)
            return Response(success=False, error=str(e))

    async def _handle_ping(self, _params: dict[str, Any]) -> Response:
        return Response(success=True, data="pong")

    async def _handle_connect(self, params: dict[str, Any]) -> Response:
        p = ConnectParams.model_validate(params)
        result = await self._wrapper.connect(p.account, p.password, p.server, p.path)
        return Response(success=True, data=result)

    async def _handle_disconnect(self, _params: dict[str, Any]) -> Response:
        await self._wrapper.disconnect()
        return Response(success=True, data="disconnected")

    async def _handle_place_order(self, params: dict[str, Any]) -> Response:
        p = PlaceOrderParams.model_validate(params)
        result = await self._wrapper.place_order(p)
        return Response(success=True, data=result.model_dump())

    async def _handle_modify(self, params: dict[str, Any]) -> Response:
        p = ModifyParams.model_validate(params)
        result = await self._wrapper.modify_position(p)
        return Response(success=True, data=result.model_dump())

    async def _handle_close(self, params: dict[str, Any]) -> Response:
        p = CloseParams.model_validate(params)
        result = await self._wrapper.close_position(p)
        return Response(success=True, data=result.model_dump())

    async def _handle_get_positions(self, params: dict[str, Any]) -> Response:
        p = GetPositionsParams.model_validate(params)
        positions = await self._wrapper.get_positions(p.symbol)
        return Response(success=True, data=[pos.model_dump() for pos in positions])

    async def _handle_get_balance(self, _params: dict[str, Any]) -> Response:
        balance = await self._wrapper.get_balance()
        return Response(success=True, data=balance.model_dump())

    async def _handle_get_history(self, params: dict[str, Any]) -> Response:
        p = GetHistoryParams.model_validate(params)
        deals = await self._wrapper.get_history(p.from_date, p.to_date)
        return Response(success=True, data=[d.model_dump() for d in deals])
