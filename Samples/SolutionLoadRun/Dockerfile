#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SolutionLoadRun/SolutionLoadRun.csproj", "SolutionLoadRun/"]
RUN dotnet restore "SolutionLoadRun/SolutionLoadRun.csproj"
COPY . .
WORKDIR "/src/SolutionLoadRun"
RUN dotnet build "SolutionLoadRun.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SolutionLoadRun.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
#RUN apt-get update
#RUN apt-get upgrade
#RUN apt-get install -y dmidecode
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SolutionLoadRun.dll"]