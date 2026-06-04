#!/usr/bin/env sh
set -eu
systemctl start hanna-core hanna-telegram hanna-web
systemctl status --no-pager hanna-core hanna-telegram hanna-web || true
