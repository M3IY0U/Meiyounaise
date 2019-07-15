FROM mcr.microsoft.com/dotnet/core/sdk:2.2-bionic
COPY . ./

RUN dotnet restore MeiyounaiseRewrite.sln
RUN dotnet build MeiyounaiseRewrite.sln