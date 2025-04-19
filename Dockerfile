FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y python3
COPY ./Sorac.Blog/Sorac.Blog.csproj ./Sorac.Blog/Sorac.Blog.csproj
RUN dotnet restore ./Sorac.Blog/Sorac.Blog.csproj
COPY ./Sorac.Blog ./Sorac.Blog
RUN dotnet publish ./Sorac.Blog/Sorac.Blog.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
COPY --from=build /app /app
WORKDIR /app
ENTRYPOINT ["/app/Sorac.Blog"]
