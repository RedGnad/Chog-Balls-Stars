# The whole game behind one address, with its judge (docs/DEPLOY.md): Node runs the services
# and serves the game, the .NET runtime runs the program that plays a match again. Nothing of
# the game is built here: site/ was assembled beforehand (tools/release/assemble_site.py)
# and carries the build, the portraits, the judge and the assets it reads.
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS dotnet

FROM node:22-bookworm-slim

# What the .NET runtime loads besides what Node's image already has.
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates libicu72 libssl3 \
 && rm -rf /var/lib/apt/lists/*
COPY --from=dotnet /usr/share/dotnet /usr/share/dotnet
RUN ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet
ENV DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /app
COPY server/package.json server/package-lock.json server/
RUN npm ci --prefix server --omit=dev
COPY server/src server/src
COPY web/hero web/hero
COPY site site

# The image is refused if its own .NET cannot play the recorded matches again and name the
# winners the game announced: a host never starts with a judge that does not work.
COPY tests/fixtures/replays tests/fixtures/replays
COPY tools/release/check_judge.sh tools/release/check_judge.sh
RUN sh tools/release/check_judge.sh dotnet

# The careers, the secrets made at the first start and the records of the matches go to
# DATA_DIR: mount a disk there, or everything in it is lost when the container is replaced.
ENV NODE_ENV=production PORT=10000 DATA_DIR=/var/data
EXPOSE 10000
CMD ["node", "server/src/host.js"]
