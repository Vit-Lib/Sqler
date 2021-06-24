FROM serset/dotnet:2.1

#(x.1)alias sqler
RUN echo '#!/bin/bash\ndotnet /root/app/Sqler.dll $*' > /usr/bin/sqler && chmod +x /usr/bin/sqler

#(x.2)app
COPY app /root/app
WORKDIR /root/app
CMD dotnet Sqler.dll