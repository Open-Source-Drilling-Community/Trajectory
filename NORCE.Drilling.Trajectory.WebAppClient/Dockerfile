FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build

ARG NORCE_NUGET_USER 
ARG NORCE_NUGET_APIKEY 
ADD ./NORCE_CA.cer /usr/local/share/ca-certificates/foo.crt
RUN chmod 644 /usr/local/share/ca-certificates/foo.crt && update-ca-certificates

WORKDIR /src
COPY ["NORCE.Drilling.Trajectory.ModelClientShared/NORCE.Drilling.Trajectory.ModelClientShared.csproj", "NORCE.Drilling.Trajectory.ModelClientShared/"]
COPY ["NORCE.Drilling.Trajectory.WebAppClient/NORCE.Drilling.Trajectory.WebApp.Client.csproj", "NORCE.Drilling.Trajectory.WebAppClient/"]
COPY ./nuget.config .
RUN dotnet restore "NORCE.Drilling.Trajectory.WebAppClient/NORCE.Drilling.Trajectory.WebApp.Client.csproj"

COPY . .
WORKDIR "/src/NORCE.Drilling.Trajectory.WebAppClient"
RUN dotnet build "NORCE.Drilling.Trajectory.WebApp.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NORCE.Drilling.Trajectory.WebApp.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NORCE.Drilling.Trajectory.WebApp.Client.dll"]
