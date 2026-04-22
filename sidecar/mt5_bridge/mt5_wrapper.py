"""Wrapper around the MetaTrader5 Python library with async-safe access."""

from __future__ import annotations

import asyncio
import logging
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
from typing import TYPE_CHECKING

from models import (
    AccountBalance,
    CloseParams,
    HistoryDeal,
    ModifyParams,
    OrderAction,
    OrderResult,
    OrderType,
    PlaceOrderParams,
    Position,
)

if TYPE_CHECKING:
    pass

logger = logging.getLogger(__name__)

# Single-threaded executor: MT5 is COM-based, all calls must go to the same thread.
_mt5_executor = ThreadPoolExecutor(max_workers=1, thread_name_prefix="mt5")


class Mt5Error(Exception):
    """Raised when an MT5 operation fails."""

    def __init__(self, code: int, message: str) -> None:
        self.code = code
        super().__init__(f"MT5 error {code}: {message}")


class Mt5Wrapper:
    """Thread-safe async wrapper for the MetaTrader5 library.

    All MT5 calls are dispatched to a single dedicated thread via ThreadPoolExecutor
    to satisfy COM threading requirements.
    """

    def __init__(self) -> None:
        self._connected = False

    async def connect(
        self,
        account: int,
        password: str,
        server: str,
        path: str | None = None,
    ) -> dict[str, object]:
        """Initialize MT5 terminal and log in."""

        def _connect() -> dict[str, object]:
            import MetaTrader5 as mt5

            kwargs: dict[str, object] = {}
            if path:
                kwargs["path"] = path

            if not mt5.initialize(**kwargs):  # type: ignore[arg-type]
                code, msg = mt5.last_error()
                raise Mt5Error(code, msg)

            if not mt5.login(account, password=password, server=server):
                code, msg = mt5.last_error()
                mt5.shutdown()
                raise Mt5Error(code, msg)

            info = mt5.account_info()
            if info is None:
                code, msg = mt5.last_error()
                raise Mt5Error(code, msg)

            return {
                "connected": True,
                "account": info.login,
                "server": info.server,
                "name": info.name,
                "balance": info.balance,
                "currency": info.currency,
            }

        result = await asyncio.get_event_loop().run_in_executor(_mt5_executor, _connect)
        self._connected = True
        logger.info("Connected to MT5 account %d", account)
        return result

    async def disconnect(self) -> None:
        """Shut down MT5 connection."""

        def _disconnect() -> None:
            import MetaTrader5 as mt5

            mt5.shutdown()

        await asyncio.get_event_loop().run_in_executor(_mt5_executor, _disconnect)
        self._connected = False
        logger.info("Disconnected from MT5")

    async def place_order(self, params: PlaceOrderParams) -> OrderResult:
        """Send a trade request to MT5."""

        def _place_order() -> OrderResult:
            import MetaTrader5 as mt5

            # Map action + order_type to MT5 constants
            type_map = {
                (OrderAction.BUY, OrderType.MARKET): mt5.ORDER_TYPE_BUY,
                (OrderAction.SELL, OrderType.MARKET): mt5.ORDER_TYPE_SELL,
                (OrderAction.BUY, OrderType.LIMIT): mt5.ORDER_TYPE_BUY_LIMIT,
                (OrderAction.SELL, OrderType.LIMIT): mt5.ORDER_TYPE_SELL_LIMIT,
                (OrderAction.BUY, OrderType.STOP): mt5.ORDER_TYPE_BUY_STOP,
                (OrderAction.SELL, OrderType.STOP): mt5.ORDER_TYPE_SELL_STOP,
            }
            order_type = type_map.get((params.action, params.order_type))
            if order_type is None:
                msg = f"Unsupported order: {params.action} {params.order_type}"
                raise Mt5Error(-1, msg)

            # Get symbol info for filling mode and price
            info = mt5.symbol_info(params.symbol)
            if info is None:
                code, msg = mt5.last_error()
                raise Mt5Error(code, msg)
            if not info.visible:
                mt5.symbol_select(params.symbol, True)

            tick = mt5.symbol_info_tick(params.symbol)
            if tick is None:
                code, msg = mt5.last_error()
                raise Mt5Error(code, msg)

            price = params.entry_price
            if price is None:
                price = tick.ask if params.action == OrderAction.BUY else tick.bid

            request: dict[str, object] = {
                "action": mt5.TRADE_ACTION_DEAL if params.order_type == OrderType.MARKET else mt5.TRADE_ACTION_PENDING,
                "symbol": params.symbol,
                "volume": params.lot_size,
                "type": order_type,
                "price": price,
                "deviation": 20,
                "magic": 123456,
                "type_filling": mt5.ORDER_FILLING_IOC,
            }
            if params.sl is not None:
                request["sl"] = params.sl
            if params.tp is not None:
                request["tp"] = params.tp
            if params.comment:
                request["comment"] = params.comment

            result = mt5.order_send(request)
            if result is None or result.retcode != mt5.TRADE_RETCODE_DONE:
                code = result.retcode if result else -1
                msg = result.comment if result else "order_send returned None"
                raise Mt5Error(code, msg)

            return OrderResult(
                ticket=result.order,
                symbol=params.symbol,
                action=params.action.value,
                volume=result.volume,
                price=result.price,
            )

        return await asyncio.get_event_loop().run_in_executor(_mt5_executor, _place_order)

    async def modify_position(self, params: ModifyParams) -> OrderResult:
        """Modify SL/TP of an open position."""

        def _modify() -> OrderResult:
            import MetaTrader5 as mt5

            positions = mt5.positions_get(ticket=params.ticket)
            if not positions:
                raise Mt5Error(-1, f"Position {params.ticket} not found")

            pos = positions[0]
            request: dict[str, object] = {
                "action": mt5.TRADE_ACTION_SLTP,
                "position": params.ticket,
                "symbol": pos.symbol,
                "sl": params.sl if params.sl is not None else pos.sl,
                "tp": params.tp if params.tp is not None else pos.tp,
            }

            result = mt5.order_send(request)
            if result is None or result.retcode != mt5.TRADE_RETCODE_DONE:
                code = result.retcode if result else -1
                msg = result.comment if result else "order_send returned None"
                raise Mt5Error(code, msg)

            return OrderResult(
                ticket=params.ticket,
                symbol=pos.symbol,
                action="Modify",
                volume=pos.volume,
                price=pos.price_open,
            )

        return await asyncio.get_event_loop().run_in_executor(_mt5_executor, _modify)

    async def close_position(self, params: CloseParams) -> OrderResult:
        """Close an open position."""

        def _close() -> OrderResult:
            import MetaTrader5 as mt5

            positions = mt5.positions_get(ticket=params.ticket)
            if not positions:
                raise Mt5Error(-1, f"Position {params.ticket} not found")

            pos = positions[0]
            close_type = mt5.ORDER_TYPE_SELL if pos.type == mt5.ORDER_TYPE_BUY else mt5.ORDER_TYPE_BUY
            tick = mt5.symbol_info_tick(pos.symbol)
            if tick is None:
                code, msg = mt5.last_error()
                raise Mt5Error(code, msg)

            price = tick.bid if pos.type == mt5.ORDER_TYPE_BUY else tick.ask
            volume = params.lot_size if params.lot_size else pos.volume

            request: dict[str, object] = {
                "action": mt5.TRADE_ACTION_DEAL,
                "position": params.ticket,
                "symbol": pos.symbol,
                "volume": volume,
                "type": close_type,
                "price": price,
                "deviation": 20,
                "magic": 123456,
                "type_filling": mt5.ORDER_FILLING_IOC,
            }

            result = mt5.order_send(request)
            if result is None or result.retcode != mt5.TRADE_RETCODE_DONE:
                code = result.retcode if result else -1
                msg = result.comment if result else "order_send returned None"
                raise Mt5Error(code, msg)

            return OrderResult(
                ticket=result.order,
                symbol=pos.symbol,
                action="Close",
                volume=volume,
                price=result.price,
            )

        return await asyncio.get_event_loop().run_in_executor(_mt5_executor, _close)

    async def get_positions(self, symbol: str | None = None) -> list[Position]:
        """Get open positions, optionally filtered by symbol."""

        def _get_positions() -> list[Position]:
            import MetaTrader5 as mt5

            if symbol:
                positions = mt5.positions_get(symbol=symbol)
            else:
                positions = mt5.positions_get()

            if positions is None:
                return []

            return [
                Position(
                    ticket=p.ticket,
                    symbol=p.symbol,
                    type="Buy" if p.type == mt5.ORDER_TYPE_BUY else "Sell",
                    volume=p.volume,
                    open_price=p.price_open,
                    current_price=p.price_current,
                    profit=p.profit,
                    sl=p.sl if p.sl != 0 else None,
                    tp=p.tp if p.tp != 0 else None,
                    time=datetime.fromtimestamp(p.time, tz=timezone.utc),
                    comment=p.comment or None,
                )
                for p in positions
            ]

        return await asyncio.get_event_loop().run_in_executor(_mt5_executor, _get_positions)

    async def get_balance(self) -> AccountBalance:
        """Get account balance information."""

        def _get_balance() -> AccountBalance:
            import MetaTrader5 as mt5

            info = mt5.account_info()
            if info is None:
                code, msg = mt5.last_error()
                raise Mt5Error(code, msg)

            return AccountBalance(
                balance=info.balance,
                equity=info.equity,
                margin=info.margin,
                free_margin=info.margin_free,
                profit=info.profit,
            )

        return await asyncio.get_event_loop().run_in_executor(_mt5_executor, _get_balance)

    async def get_history(self, from_date: datetime, to_date: datetime) -> list[HistoryDeal]:
        """Get historical deals within a date range."""

        def _get_history() -> list[HistoryDeal]:
            import MetaTrader5 as mt5

            deals = mt5.history_deals_get(from_date, to_date)
            if deals is None:
                return []

            return [
                HistoryDeal(
                    ticket=d.ticket,
                    symbol=d.symbol,
                    type="Buy" if d.type == mt5.DEAL_TYPE_BUY else "Sell",
                    volume=d.volume,
                    price=d.price,
                    profit=d.profit,
                    time=datetime.fromtimestamp(d.time, tz=timezone.utc),
                )
                for d in deals
            ]

        return await asyncio.get_event_loop().run_in_executor(_mt5_executor, _get_history)

    @property
    def is_connected(self) -> bool:
        """Whether the wrapper believes it is connected."""
        return self._connected
