FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

COPY ./src /src

RUN dotnet publish ./src/zombiefollower.csproj -o /bin

FROM mcr.microsoft.com/dotnet/runtime:7.0 

COPY --from=build /bin /

ENTRYPOINT ["dotnet", "zombiefollower.dll"]
