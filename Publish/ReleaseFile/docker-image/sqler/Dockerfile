FROM serset/dotnet:2.1

#(x.1)alias sqler
RUN echo '#!/bin/bash\ndotnet /root/app/FileZip.dll $*' > /usr/bin/sqler && chmod +x /usr/bin/sqler

#(x.2)appCOPY app /root/app
WORKDIR /root/app
CMD dotnet Sqler.dll