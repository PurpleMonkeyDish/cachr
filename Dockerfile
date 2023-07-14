ARG CONFIGURATION=Release
ARG PROJECT
ARG DOTNET_SDK_VERSION="7.0"
ARG DOTNET_RUNTIME_VERSION="7.0"

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:$DOTNET_SDK_VERSION AS sdk-base
ARG DOCKER_USER=default_user
ENV PATH="${PATH}:~/.dotnet/tools"
RUN addgroup --system $DOCKER_USER && adduser --system $DOCKER_USER && addgroup ${DOCKER_USER} ${DOCKER_USER}
RUN mkdir /source
RUN mkdir /output
RUN chown ${DOCKER_USER}:${DOCKER_USER} /source
RUN chown ${DOCKER_USER}:${DOCKER_USER} /output
USER ${DOCKER_USER}

FROM --platform=$BUILDPLATFORM sdk-base AS prepare-restore-files
ARG PROJECT
ARG DOCKER_USER=default_user
USER ${DOCKER_USER}
RUN dotnet tool install --global dotnet-subset
WORKDIR /source
COPY --chown=${DOCKER_USER}:${DOCKER_USER} . .
RUN dotnet subset restore ${PROJECT}/${PROJECT}.csproj --root-directory /source --output restore_files/


FROM --platform=$BUILDPLATFORM sdk-base AS build
ARG CONFIGURATION
ARG PROJECT
ARG DOCKER_USER=default_user
USER ${DOCKER_USER}
WORKDIR /source
COPY --from=prepare-restore-files --chown=${DOCKER_USER}:${DOCKER_USER} /source/restore_files .
RUN dotnet restore ${PROJECT}/${PROJECT}.csproj -p:Configuration="$CONFIGURATION"
COPY --chown=${DOCKER_USER}:${DOCKER_USER} . .
RUN dotnet publish --no-restore ${PROJECT}/${PROJECT}.csproj -c $CONFIGURATION -o /output



FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET_RUNTIME_VERSION AS final
ARG PROJECT
ARG DOCKER_USER=default_user
LABEL org.opencontainers.image.source="https://github.com/PurpleMonkeyDish/cachr"
RUN addgroup --system $DOCKER_USER && adduser --system $DOCKER_USER && addgroup ${DOCKER_USER} ${DOCKER_USER}
USER ${DOCKER_USER}
ENV STARTUP_PROJECT=$PROJECT
WORKDIR /app
COPY --from=build --chown=${DOCKER_USER}:${DOCKER_USER} /output .
ENTRYPOINT ./$STARTUP_PROJECT
