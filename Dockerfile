# Copyright 2021 Yubico AB
# 
# Licensed under the Apache License, Version 2.0 (the "License").
# You may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

FROM nginx:alpine

ARG UID=1000
ARG GID=1000

# -h - Home directory
# -S - Create a system user/group
# -s - Login shell
# -H - Don't create home directory
RUN addgroup -g ${GID} -S ynginx \
    && adduser -h /nonexistent -s /bin/false -G ynginx -S -H -u ${UID} ynginx

COPY --chown=0:0 docs/_site /usr/share/nginx/www/
COPY --chown=0:0 nginx.conf /etc/nginx/nginx.conf

USER ${UID}:${GID}

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
