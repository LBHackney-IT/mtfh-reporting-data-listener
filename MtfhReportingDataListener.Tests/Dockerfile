FROM mcr.microsoft.com/dotnet/core/sdk:3.1

# disable microsoft telematry
ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

WORKDIR /app

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
