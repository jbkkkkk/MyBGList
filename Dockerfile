
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder 

WORKDIR /Application

EXPOSE 8080


COPY . ./

RUN dotnet restore

RUN dotnet publish -c Release -o output


FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /Application

COPY --from=builder /Application/output .

ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "MyBGList.dll"]