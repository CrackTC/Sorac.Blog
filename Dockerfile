FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y python3
COPY ./Sorac.Blog.Client/Sorac.Blog.Client.csproj ./Sorac.Blog.Client/Sorac.Blog.Client.csproj
RUN dotnet workload restore ./Sorac.Blog.Client/Sorac.Blog.Client.csproj && \
    dotnet restore ./Sorac.Blog.Client/Sorac.Blog.Client.csproj
COPY ./Sorac.Blog.Client ./Sorac.Blog.Client
RUN dotnet publish ./Sorac.Blog.Client/Sorac.Blog.Client.csproj -c Release

COPY ./Sorac.Blog/Sorac.Blog.csproj ./Sorac.Blog/Sorac.Blog.csproj
RUN dotnet restore ./Sorac.Blog/Sorac.Blog.csproj
COPY ./Sorac.Blog ./Sorac.Blog
RUN dotnet publish ./Sorac.Blog/Sorac.Blog.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
COPY --from=build /app /app
WORKDIR /app
ENTRYPOINT ["/app/Sorac.Blog"]
