#!/usr/bin/env sh
set -eu

BASE_URL="${BASE_URL:-http://localhost:8080}"
EMAIL="${DEMO_USER_EMAIL:-demo@newsintel.dev}"
PASSWORD="${DEMO_USER_PASSWORD:-Demo123!}"

echo "[1/3] Checking health..."
curl --fail --silent --show-error "$BASE_URL/health" >/dev/null

echo "[2/3] Checking frontend..."
curl --fail --silent --show-error "$BASE_URL/" >/dev/null

echo "[3/3] Checking demo login..."
response=$(curl --fail --silent --show-error \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" \
  "$BASE_URL/api/auth/login")
printf '%s' "$response" | grep -q 'accessToken'

echo "Smoke test passed."
