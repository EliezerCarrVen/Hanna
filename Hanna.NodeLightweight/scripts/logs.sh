#!/usr/bin/env sh
set -eu
journalctl -u hanna-core -u hanna-telegram -u hanna-web --no-pager -n "${1:-80}"
