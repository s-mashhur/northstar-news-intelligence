SHELL := /bin/sh

.PHONY: up down logs test-backend typecheck build-frontend smoke

up:
	docker compose up --build -d

down:
	docker compose down

logs:
	docker compose logs -f --tail=150

test-backend:
	dotnet test backend/NewsIntelligence.sln

typecheck:
	cd frontend && npm run typecheck

build-frontend:
	cd frontend && npm run build

smoke:
	./scripts/smoke.sh
