# MT5 Bridge Sidecar

Python process that bridges TVBridge to MetaTrader 5 using the official MetaQuotes `MetaTrader5` package. Communicates with the C# host via ZeroMQ.

## Requirements

- Python 3.11+
- MetaTrader 5 terminal installed and logged in
- See `requirements.txt` for Python dependencies

## Usage

Normally launched automatically by the TVBridge app. For standalone testing:

```bash
pip install -r requirements.txt
python main.py
```
