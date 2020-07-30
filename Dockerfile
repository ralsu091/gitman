FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder
WORKDIR /wtf
COPY . .
RUN dotnet restore
RUN dotnet publish -c release -o gitman --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY --from=builder ./wtf/gitman .
RUN ls -l
ENTRYPOINT [ "./gitman" ]