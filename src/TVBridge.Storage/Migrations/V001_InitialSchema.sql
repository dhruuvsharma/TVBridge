-- UP

CREATE TABLE IF NOT EXISTS signals (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    alert_id        TEXT    NOT NULL,
    strategy_id     TEXT    NOT NULL,
    account_tag     TEXT    NOT NULL,
    symbol          TEXT    NOT NULL,
    action          TEXT    NOT NULL,
    order_type      TEXT    NOT NULL,
    entry_price     REAL,
    stop_loss       REAL,
    take_profit     REAL,
    lot_size        REAL,
    risk_percent    REAL,
    timeframe       TEXT    NOT NULL,
    timestamp       TEXT    NOT NULL,
    comment         TEXT,
    received_at     TEXT    NOT NULL DEFAULT (datetime('now')),
    processed       INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_signals_alert_id ON signals(alert_id);
CREATE INDEX IF NOT EXISTS idx_signals_strategy_id ON signals(strategy_id);
CREATE INDEX IF NOT EXISTS idx_signals_received_at ON signals(received_at);

CREATE TABLE IF NOT EXISTS rules (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    name              TEXT    NOT NULL,
    strategy_id       TEXT,
    symbol            TEXT,
    action            TEXT,
    account_tag       TEXT,
    timeframe         TEXT,
    destination_ids   TEXT    NOT NULL,
    priority          INTEGER NOT NULL DEFAULT 0,
    continue_on_match INTEGER NOT NULL DEFAULT 0,
    dry_run_override  INTEGER,
    lot_multiplier    REAL    NOT NULL DEFAULT 1.0,
    enabled           INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_rules_priority ON rules(priority);

CREATE TABLE IF NOT EXISTS channels (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    name             TEXT    NOT NULL,
    channel_type     TEXT    NOT NULL,
    encrypted_config BLOB,
    enabled          INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS audit_log (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp   TEXT    NOT NULL DEFAULT (datetime('now')),
    signal_id   INTEGER,
    rule_id     INTEGER,
    channel_id  INTEGER,
    action      TEXT    NOT NULL,
    result      TEXT,
    FOREIGN KEY (signal_id)  REFERENCES signals(id),
    FOREIGN KEY (rule_id)    REFERENCES rules(id),
    FOREIGN KEY (channel_id) REFERENCES channels(id)
);

CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON audit_log(timestamp);

CREATE TABLE IF NOT EXISTS settings (
    key             TEXT PRIMARY KEY,
    encrypted_value BLOB
);

-- DOWN

DROP TABLE IF EXISTS settings;
DROP TABLE IF EXISTS audit_log;
DROP TABLE IF EXISTS channels;
DROP TABLE IF EXISTS rules;
DROP TABLE IF EXISTS signals;
