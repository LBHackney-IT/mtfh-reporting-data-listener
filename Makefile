include .env

.PHONY: setup
setup:
	docker-compose build

.PHONY: build
build:
	LBHPACKAGESTOKEN=${LBHPACKAGESTOKEN} docker-compose build mtfh-reporting-data-listener

.PHONY: serve
serve:
	docker-compose build mtfh-reporting-data-listener && docker-compose up mtfh-reporting-data-listener

.PHONY: shell
shell:
	docker-compose run mtfh-reporting-data-listener bash

.PHONY: test
test:
	LBHPACKAGESTOKEN=${LBHPACKAGESTOKEN} docker-compose build mtfh-reporting-data-listener-test && docker-compose run mtfh-reporting-data-listener-test 

.PHONY: lint
lint:
	-dotnet tool install -g dotnet-format --version 5.1.250801
	dotnet tool update -g dotnet-format --version 5.1.250801
	dotnet format

./MtfhReportingDataListener/bin/release/netcoreapp3.1/mtfh-reporting-data-listener.zip:
	chmod +x ./MtfhReportingDataListener/build.sh
	cd ./MtfhReportingDataListener && ./build.sh

.PHONY: deploy-sls
deploy-sls: ./MtfhReportingDataListener/bin/release/netcoreapp3.1/mtfh-reporting-data-listener.zip
	cd ./MtfhReportingDataListener/ && aws-vault exec hackney-dev-scratch -- sls deploy --stage development --conceal

.PHONY: deploy-tf
deploy-tf:
	cd terraform/development && aws-vault exec hackney-dev-scratch -- terraform init
	cd terraform/development && aws-vault exec hackney-dev-scratch -- terraform apply

.PHONY: deploy-dev
deploy-dev: deploy-tf deploy-sls
	
