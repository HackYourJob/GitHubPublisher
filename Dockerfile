FROM mono:4

WORKDIR /var/app

COPY ./ /var/app

RUN sh restore.sh

CMD mono packages/FAKE/tools/FAKE.exe run.fsx