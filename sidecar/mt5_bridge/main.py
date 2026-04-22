"""TVBridge MT5 Sidecar — bridges C# app to MetaTrader 5 via ZeroMQ."""

from __future__ import annotations

import argparse
import asyncio
import logging
import signal
import sys

from server import Mt5Server


def parse_args() -> argparse.Namespace:
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(description="TVBridge MT5 Sidecar")
    parser.add_argument("--rep-port", type=int, default=5556, help="ZMQ REP port (default: 5556)")
    parser.add_argument("--pub-port", type=int, default=5557, help="ZMQ PUB port (default: 5557)")
    parser.add_argument("--state-interval", type=float, default=1.0, help="Account state publish interval in seconds")
    parser.add_argument("--log-level", default="INFO", choices=["DEBUG", "INFO", "WARNING", "ERROR"])
    parser.add_argument("--health-check", action="store_true", help="Check if sidecar is running and exit")
    return parser.parse_args()


def run_health_check(rep_port: int) -> int:
    """Connect to REP socket, send ping, return 0 if healthy."""
    import zmq

    ctx = zmq.Context()
    sock = ctx.socket(zmq.REQ)
    sock.setsockopt(zmq.RCVTIMEO, 3000)
    sock.setsockopt(zmq.LINGER, 0)
    try:
        sock.connect(f"tcp://127.0.0.1:{rep_port}")
        sock.send_string('{"command":"ping","params":{}}')
        reply = sock.recv_string()
        import json

        data = json.loads(reply)
        if data.get("success"):
            print("OK")
            return 0
        print(f"FAIL: {data.get('error', 'unknown')}")
        return 1
    except zmq.ZMQError as e:
        print(f"FAIL: {e}")
        return 1
    finally:
        sock.close()
        ctx.term()


def main() -> None:
    """Entry point for the MT5 sidecar."""
    args = parse_args()

    if args.health_check:
        sys.exit(run_health_check(args.rep_port))

    logging.basicConfig(
        level=getattr(logging, args.log_level),
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        stream=sys.stdout,
    )

    server = Mt5Server(
        rep_port=args.rep_port,
        pub_port=args.pub_port,
        state_interval=args.state_interval,
    )

    loop = asyncio.new_event_loop()

    def _shutdown() -> None:
        loop.create_task(server.stop())

    # Handle graceful shutdown on Windows and Unix
    if sys.platform == "win32":
        signal.signal(signal.SIGINT, lambda *_: _shutdown())
        signal.signal(signal.SIGTERM, lambda *_: _shutdown())
    else:
        loop.add_signal_handler(signal.SIGINT, _shutdown)
        loop.add_signal_handler(signal.SIGTERM, _shutdown)

    try:
        loop.run_until_complete(server.run())
    except KeyboardInterrupt:
        loop.run_until_complete(server.stop())
    finally:
        loop.close()


if __name__ == "__main__":
    main()
