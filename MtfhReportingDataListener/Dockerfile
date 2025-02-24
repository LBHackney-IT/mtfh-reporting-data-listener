FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG LBHPACKAGESTOKEN
ENV LBHPACKAGESTOKEN=$LBHPACKAGESTOKEN
WORKDIR /app

# Copy csproj and NUGet.Config and restore as distinct layers
COPY ./MtfhReportingDataListener/MtfhReportingDataListener.csproj ./
COPY /NuGet.Config /root/.nuget/NuGet/NuGet.Config
RUN dotnet restore ./MtfhReportingDataListener.csproj


# Copy everything else and build
COPY ./MtfhReportingDataListener ./
RUN dotnet build -c Release -o out ./MtfhReportingDataListener.csproj
RUN dotnet publish -c Release -o out ./MtfhReportingDataListener.csproj

EXPOSE ${PORT:-3000}
CMD ASPNETCORE_URLS=http://+:${PORT:-3000} dotnet ./out/MtfhReportingDataListener.dll
