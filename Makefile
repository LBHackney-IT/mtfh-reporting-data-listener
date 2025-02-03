include .env

.PHONY: setup
setup:
	docker compose build

.PHONY: build
build:
	LBHPACKAGESTOKEN=${LBHPACKAGESTOKEN} SONAR_TOKEN=${SONAR_TOKEN} docker compose build mtfh-reporting-data-listener

.PHONY: serve
serve:
	docker compose build mtfh-reporting-data-listener && docker compose up mtfh-reporting-data-listener

.PHONY: shell
shell:
	docker compose run mtfh-reporting-data-listener bash

.PHONY: test
test:
	LBHPACKAGESTOKEN=${LBHPACKAGESTOKEN} && SONAR_TOKEN=${SONAR_TOKEN} docker compose build mtfh-reporting-data-listener-test && docker compose run mtfh-reporting-data-listener-test 

.PHONY: lint
lint:
	-dotnet tool install -g dotnet-format --version 5.1.250801
	dotnet tool update -g dotnet-format --version 5.1.250801
	dotnet format