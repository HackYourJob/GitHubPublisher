FROM ubuntu:xenial

RUN apt-get update && \
    apt-get install -y git apt-transport-https ca-certificates && \
    apt-key adv --keyserver hkp://p80.pool.sks-keyservers.net:80 --recv-keys 58118E89F3A912897C070ADBF76221572C52609D && \
    echo "deb https://apt.dockerproject.org/repo ubuntu-xenial main" > /etc/apt/sources.list.d/docker.list && \
    apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    echo "deb http://download.mono-project.com/repo/debian beta main" | tee /etc/apt/sources.list.d/mono-xamarin.list && \
    apt-get update && \
    apt-get purge lxc-docker && \
    apt-get install -y docker-engine mono-devel fsharp curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /var/app

COPY ./ /var/app

RUN sh restore.sh

CMD mono packages/FAKE/tools/FAKE.exe run.fsx

