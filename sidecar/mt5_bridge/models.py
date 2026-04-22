"""Pydantic models for the MT5 sidecar ZeroMQ protocol."""

from __future__ import annotations

from datetime import datetime
from enum import StrEnum
from typing import Any

from pydantic import BaseModel, Field


class Command(StrEnum):
    """Available sidecar commands."""

    CONNECT = "connect"
    DISCONNECT = "disconnect"
    PLACE_ORDER = "place_order"
    MODIFY = "modify"
    CLOSE = "close"
    GET_POSITIONS = "get_positions"
    GET_BALANCE = "get_balance"
    GET_HISTORY = "get_history"
    PING = "ping"


class OrderAction(StrEnum):
    """Trade direction."""

    BUY = "Buy"
    SELL = "Sell"


class OrderType(StrEnum):
    """Order type."""

    MARKET = "Market"
    LIMIT = "Limit"
    STOP = "Stop"


# --- Request models ---


class Request(BaseModel):
    """Top-level ZMQ request envelope."""

    command: Command
    params: dict[str, Any] = Field(default_factory=dict)


class ConnectParams(BaseModel):
    """Parameters for the connect command."""

    account: int
    password: str
    server: str
    path: str | None = None


class PlaceOrderParams(BaseModel):
    """Parameters for the place_order command."""

    symbol: str
    action: OrderAction
    order_type: OrderType = OrderType.MARKET
    lot_size: float
    entry_price: float | None = None
    sl: float | None = None
    tp: float | None = None
    comment: str | None = None


class ModifyParams(BaseModel):
    """Parameters for the modify command."""

    ticket: int
    sl: float | None = None
    tp: float | None = None


class CloseParams(BaseModel):
    """Parameters for the close command."""

    ticket: int
    lot_size: float | None = None


class GetPositionsParams(BaseModel):
    """Parameters for the get_positions command."""

    symbol: str | None = None


class GetHistoryParams(BaseModel):
    """Parameters for the get_history command."""

    from_date: datetime
    to_date: datetime


# --- Response models ---


class Response(BaseModel):
    """Top-level ZMQ response envelope."""

    success: bool
    data: Any = None
    error: str | None = None


class OrderResult(BaseModel):
    """Result of a trade operation."""

    ticket: int
    symbol: str
    action: str
    volume: float
    price: float


class Position(BaseModel):
    """An open MT5 position."""

    ticket: int
    symbol: str
    type: str
    volume: float
    open_price: float
    current_price: float
    profit: float
    sl: float | None = None
    tp: float | None = None
    time: datetime | None = None
    comment: str | None = None


class AccountBalance(BaseModel):
    """MT5 account balance info."""

    balance: float
    equity: float
    margin: float
    free_margin: float
    profit: float


class HistoryDeal(BaseModel):
    """A historical MT5 deal."""

    ticket: int
    symbol: str
    type: str
    volume: float
    price: float
    profit: float
    time: datetime


class AccountState(BaseModel):
    """Published account state snapshot."""

    positions: list[Position]
    balance: AccountBalance
    timestamp: datetime
