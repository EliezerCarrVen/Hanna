#!/usr/bin/env sh
set -eu
systemctl status --no-pager hanna-core hanna-telegram hanna-web || true
