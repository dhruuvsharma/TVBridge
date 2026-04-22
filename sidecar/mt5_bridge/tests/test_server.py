"""Tests for the MT5 sidecar ZeroMQ server with mocked MT5 wrapper."""

from __future__ import annotations

import asyncio
import json
import sys
from pathlib import Path
from unittest.mock import AsyncMock, patch

import pytest
import zmq
import zmq.asyncio

sys.path.insert(0, str(Path(__file__).parent.parent))

from models import AccountBalance, OrderResult, Position, Response
from server import Mt5Server


def _find_free_port() -> int:
    """Find a free TCP port."""
    import socket

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


@pytest.fixture()
def ports() -> tuple[int, int]:
    return _find_free_port(), _find_free_port()


async def _send_command(rep_port: int, command: str, params: dict | None = None) -> dict:
    """Send a command to the server and return the response."""
    ctx = zmq.asyncio.Context()
    sock = ctx.socket(zmq.REQ)
    sock.connect(f"tcp://127.0.0.1:{rep_port}")
    try:
        request = json.dumps({"command": command, "params": params or {}})
        await sock.send_string(request)
        reply = await asyncio.wait_for(sock.recv_string(), timeout=5.0)
        return json.loads(reply)
    finally:
        sock.close()
        ctx.term()


class TestPing:
    @pytest.mark.asyncio()
    async def test_ping_returns_pong(self, ports: tuple[int, int]) -> None:
        rep_port, pub_port = ports
        server = Mt5Server(rep_port=rep_port, pub_port=pub_port)

        server_task = asyncio.create_task(server.run())
        await asyncio.sleep(0.3)  # Let server bind

        try:
            result = await _send_command(rep_port, "ping")
            assert result["success"] is True
            assert result["data"] == "pong"
        finally:
            await server.stop()
            await asyncio.sleep(0.2)
            server_task.cancel()
            try:
                await server_task
            except asyncio.CancelledError:
                pass


class TestInvalidRequest:
    @pytest.mark.asyncio()
    async def test_malformed_json(self, ports: tuple[int, int]) -> None:
        rep_port, pub_port = ports
        server = Mt5Server(rep_port=rep_port, pub_port=pub_port)

        server_task = asyncio.create_task(server.run())
        await asyncio.sleep(0.3)

        ctx = zmq.asyncio.Context()
        sock = ctx.socket(zmq.REQ)
        sock.connect(f"tcp://127.0.0.1:{rep_port}")

        try:
            await sock.send_string("not json at all")
            reply = await asyncio.wait_for(sock.recv_string(), timeout=5.0)
            result = json.loads(reply)
            assert result["success"] is False
            assert "Invalid request" in result["error"]
        finally:
            sock.close()
            ctx.term()
            await server.stop()
            await asyncio.sleep(0.2)
            server_task.cancel()
            try:
                await server_task
            except asyncio.CancelledError:
                pass

    @pytest.mark.asyncio()
    async def test_unknown_command(self, ports: tuple[int, int]) -> None:
        rep_port, pub_port = ports
        server = Mt5Server(rep_port=rep_port, pub_port=pub_port)

        server_task = asyncio.create_task(server.run())
        await asyncio.sleep(0.3)

        ctx = zmq.asyncio.Context()
        sock = ctx.socket(zmq.REQ)
        sock.connect(f"tcp://127.0.0.1:{rep_port}")

        try:
            # Send a valid JSON but with a command not in the Command enum
            await sock.send_string('{"command":"nonexistent","params":{}}')
            reply = await asyncio.wait_for(sock.recv_string(), timeout=5.0)
            result = json.loads(reply)
            assert result["success"] is False
        finally:
            sock.close()
            ctx.term()
            await server.stop()
            await asyncio.sleep(0.2)
            server_task.cancel()
            try:
                await server_task
            except asyncio.CancelledError:
                pass


class TestCommandDispatch:
    @pytest.mark.asyncio()
    async def test_get_balance_dispatches_to_wrapper(self, ports: tuple[int, int]) -> None:
        rep_port, pub_port = ports
        server = Mt5Server(rep_port=rep_port, pub_port=pub_port)

        mock_balance = AccountBalance(balance=10000, equity=10500, margin=200, free_margin=10300, profit=500)

        with patch.object(server._wrapper, "get_balance", new_callable=AsyncMock, return_value=mock_balance):
            with patch.object(server._wrapper, "_connected", True):
                server_task = asyncio.create_task(server.run())
                await asyncio.sleep(0.3)

                try:
                    result = await _send_command(rep_port, "get_balance")
                    assert result["success"] is True
                    assert result["data"]["balance"] == 10000
                    assert result["data"]["equity"] == 10500
                finally:
                    await server.stop()
                    await asyncio.sleep(0.2)
                    server_task.cancel()
                    try:
                        await server_task
                    except asyncio.CancelledError:
                        pass

    @pytest.mark.asyncio()
    async def test_get_positions_empty(self, ports: tuple[int, int]) -> None:
        rep_port, pub_port = ports
        server = Mt5Server(rep_port=rep_port, pub_port=pub_port)

        with patch.object(server._wrapper, "get_positions", new_callable=AsyncMock, return_value=[]):
            server_task = asyncio.create_task(server.run())
            await asyncio.sleep(0.3)

            try:
                result = await _send_command(rep_port, "get_positions")
                assert result["success"] is True
                assert result["data"] == []
            finally:
                await server.stop()
                await asyncio.sleep(0.2)
                server_task.cancel()
                try:
                    await server_task
                except asyncio.CancelledError:
                    pass

    @pytest.mark.asyncio()
    async def test_place_order_returns_result(self, ports: tuple[int, int]) -> None:
        rep_port, pub_port = ports
        server = Mt5Server(rep_port=rep_port, pub_port=pub_port)

        mock_result = OrderResult(ticket=12345, symbol="EURUSD", action="Buy", volume=0.1, price=1.1234)

        with patch.object(server._wrapper, "place_order", new_callable=AsyncMock, return_value=mock_result):
            server_task = asyncio.create_task(server.run())
            await asyncio.sleep(0.3)

            try:
                result = await _send_command(rep_port, "place_order", {
                    "symbol": "EURUSD",
                    "action": "Buy",
                    "lot_size": 0.1,
                })
                assert result["success"] is True
                assert result["data"]["ticket"] == 12345
            finally:
                await server.stop()
                await asyncio.sleep(0.2)
                server_task.cancel()
                try:
                    await server_task
                except asyncio.CancelledError:
                    pass
