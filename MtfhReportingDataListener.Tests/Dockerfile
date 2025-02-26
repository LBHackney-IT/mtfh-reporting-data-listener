FROM mcr.microsoft.com/dotnet/sdk:8.0

# disable microsoft telematry
ENV DOTNET_CLI_TELEMETRY_OPTOUT='true'

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN

WORKDIR /app

# Copy everything else and build
COPY . .

RUN dotnet restore

RUN dotnet build --no-restore

ENTRYPOINT ["dotnet", "test", "--collect", "XPlat Code Coverage;Format=opencover", "--results-directory", "./coverage"]
