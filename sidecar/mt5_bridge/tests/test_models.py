"""Tests for the MT5 sidecar protocol models."""

from __future__ import annotations

import sys
from datetime import datetime, timezone
from pathlib import Path

import pytest

# Add parent directory to path so we can import sidecar modules
sys.path.insert(0, str(Path(__file__).parent.parent))

from models import (
    AccountBalance,
    AccountState,
    CloseParams,
    Command,
    ConnectParams,
    GetHistoryParams,
    GetPositionsParams,
    HistoryDeal,
    ModifyParams,
    OrderAction,
    OrderResult,
    OrderType,
    PlaceOrderParams,
    Position,
    Request,
    Response,
)


class TestCommand:
    def test_all_commands_are_strings(self) -> None:
        for cmd in Command:
            assert isinstance(cmd.value, str)

    def test_command_values(self) -> None:
        assert Command.CONNECT == "connect"
        assert Command.PLACE_ORDER == "place_order"
        assert Command.PING == "ping"


class TestRequest:
    def test_round_trip(self) -> None:
        req = Request(command=Command.PING, params={})
        json_str = req.model_dump_json()
        parsed = Request.model_validate_json(json_str)
        assert parsed.command == Command.PING
        assert parsed.params == {}

    def test_request_with_params(self) -> None:
        req = Request(command=Command.CONNECT, params={"account": 12345, "password": "pass", "server": "srv"})
        json_str = req.model_dump_json()
        parsed = Request.model_validate_json(json_str)
        assert parsed.params["account"] == 12345


class TestConnectParams:
    def test_required_fields(self) -> None:
        p = ConnectParams(account=12345, password="pass", server="Demo")
        assert p.account == 12345
        assert p.path is None

    def test_optional_path(self) -> None:
        p = ConnectParams(account=12345, password="pass", server="Demo", path="C:/MT5/terminal64.exe")
        assert p.path == "C:/MT5/terminal64.exe"


class TestPlaceOrderParams:
    def test_market_buy(self) -> None:
        p = PlaceOrderParams(symbol="EURUSD", action=OrderAction.BUY, lot_size=0.1)
        assert p.order_type == OrderType.MARKET
        assert p.sl is None

    def test_limit_sell(self) -> None:
        p = PlaceOrderParams(
            symbol="GBPUSD",
            action=OrderAction.SELL,
            order_type=OrderType.LIMIT,
            lot_size=0.5,
            entry_price=1.2500,
            sl=1.2600,
            tp=1.2400,
        )
        assert p.entry_price == 1.25


class TestModifyParams:
    def test_round_trip(self) -> None:
        p = ModifyParams(ticket=100, sl=1.1, tp=1.2)
        parsed = ModifyParams.model_validate_json(p.model_dump_json())
        assert parsed.ticket == 100


class TestCloseParams:
    def test_partial_close(self) -> None:
        p = CloseParams(ticket=200, lot_size=0.05)
        assert p.lot_size == 0.05

    def test_full_close(self) -> None:
        p = CloseParams(ticket=200)
        assert p.lot_size is None


class TestGetPositionsParams:
    def test_no_filter(self) -> None:
        p = GetPositionsParams()
        assert p.symbol is None

    def test_symbol_filter(self) -> None:
        p = GetPositionsParams(symbol="EURUSD")
        assert p.symbol == "EURUSD"


class TestGetHistoryParams:
    def test_round_trip(self) -> None:
        now = datetime.now(tz=timezone.utc)
        p = GetHistoryParams(from_date=now, to_date=now)
        parsed = GetHistoryParams.model_validate_json(p.model_dump_json())
        assert parsed.from_date == now


class TestResponse:
    def test_success(self) -> None:
        r = Response(success=True, data="pong")
        assert r.success is True
        assert r.error is None

    def test_error(self) -> None:
        r = Response(success=False, error="connection failed")
        assert r.success is False
        assert r.data is None


class TestOrderResult:
    def test_round_trip(self) -> None:
        r = OrderResult(ticket=123, symbol="EURUSD", action="Buy", volume=0.1, price=1.1234)
        parsed = OrderResult.model_validate_json(r.model_dump_json())
        assert parsed.ticket == 123


class TestPosition:
    def test_round_trip(self) -> None:
        p = Position(
            ticket=1,
            symbol="EURUSD",
            type="Buy",
            volume=0.1,
            open_price=1.1,
            current_price=1.2,
            profit=100.0,
        )
        parsed = Position.model_validate_json(p.model_dump_json())
        assert parsed.profit == 100.0
        assert parsed.sl is None


class TestAccountBalance:
    def test_round_trip(self) -> None:
        b = AccountBalance(balance=10000, equity=10500, margin=200, free_margin=10300, profit=500)
        parsed = AccountBalance.model_validate_json(b.model_dump_json())
        assert parsed.equity == 10500


class TestHistoryDeal:
    def test_round_trip(self) -> None:
        d = HistoryDeal(
            ticket=99,
            symbol="USDJPY",
            type="Buy",
            volume=1.0,
            price=150.0,
            profit=25.0,
            time=datetime(2025, 1, 1, tzinfo=timezone.utc),
        )
        parsed = HistoryDeal.model_validate_json(d.model_dump_json())
        assert parsed.symbol == "USDJPY"


class TestAccountState:
    def test_round_trip(self) -> None:
        state = AccountState(
            positions=[
                Position(
                    ticket=1, symbol="EURUSD", type="Buy",
                    volume=0.1, open_price=1.1, current_price=1.2, profit=100,
                )
            ],
            balance=AccountBalance(balance=10000, equity=10100, margin=100, free_margin=10000, profit=100),
            timestamp=datetime.now(tz=timezone.utc),
        )
        parsed = AccountState.model_validate_json(state.model_dump_json())
        assert len(parsed.positions) == 1
        assert parsed.balance.balance == 10000
