FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

COPY ./src ./src

WORKDIR /app/src

RUN dotnet restore
RUN dotnet publish -c Release -o out

EXPOSE 8080

CMD ["dotnet", "out/src.dll"]