.PHONY: setup
setup:
	docker-compose build

.PHONY: build
build:
	docker-compose build mtfh-reporting-data-listener

.PHONY: serve
serve:
	docker-compose build mtfh-reporting-data-listener && docker-compose up mtfh-reporting-data-listener

.PHONY: shell
shell:
	docker-compose run mtfh-reporting-data-listener bash

.PHONY: test
test:
	docker-compose up dynamodb-database & docker-compose build mtfh-reporting-data-listener-test && docker-compose up mtfh-reporting-data-listener-test

.PHONY: lint
lint:
	-dotnet tool install -g dotnet-format
	dotnet tool update -g dotnet-format
	dotnet format

.PHONY: restart-db
restart-db:
	docker stop $$(docker ps -q --filter ancestor=dynamodb-database -a)
	-docker rm $$(docker ps -q --filter ancestor=dynamodb-database -a)
	docker rmi dynamodb-database
	docker-compose up -d dynamodb-database
