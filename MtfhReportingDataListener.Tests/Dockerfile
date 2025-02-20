FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# disable microsoft telematry
ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

ARG SONAR_TOKEN
ENV SONAR_TOKEN=$SONAR_TOKEN

WORKDIR /app

RUN apt-get update && apt-get install -y openjdk-17-jdk
RUN dotnet tool install --global dotnet-sonarscanner --version 5.6.0
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet sonarscanner begin /k:"LBHackney-IT_mtfh-reporting-data-listener" /o:"lbhackney-it" /d:sonar.host.url=https://sonarcloud.io /d:sonar.login="${SONAR_TOKEN}"


# Copy csproj and restore as distinct layers
COPY ./MtfhReportingDataListener.sln ./
COPY ./MtfhReportingDataListener/MtfhReportingDataListener.csproj ./MtfhReportingDataListener/
COPY ./MtfhReportingDataListener.Tests/MtfhReportingDataListener.Tests.csproj ./MtfhReportingDataListener.Tests/
COPY /NuGet.Config /root/.nuget/NuGet/NuGet.Config

RUN dotnet restore ./MtfhReportingDataListener/MtfhReportingDataListener.csproj
RUN dotnet restore ./MtfhReportingDataListener.Tests/MtfhReportingDataListener.Tests.csproj

# Copy everything else and build
COPY . .

RUN dotnet build -c Release -o out MtfhReportingDataListener/MtfhReportingDataListener.csproj
RUN dotnet build -c debug -o out MtfhReportingDataListener.Tests/MtfhReportingDataListener.Tests.csproj

RUN chmod +x ./wait-until-schemaregistry-up.sh

CMD ./wait-until-schemaregistry-up.sh && dotnet test
RUN dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"
