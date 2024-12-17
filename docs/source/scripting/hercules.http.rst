.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

hercules.http
***************
 
The **hercules.http** variable provides HTTP API.


get
----------------

.. code-block:: javascript

   hercules.http.get(url, options) 

This function executes HTTP GET request for the given URL.

- *url* - HTTP request URL
- *options* - optional options object (see hercules.http.request for details)


**Example:** Get the list of CouchDB databases on the current server.

.. code-block:: javascript

   const dbs = hercules.http.get(hercules.project.url + '_all_dbs', {username: hercules.project.username, password: password});

post
----------------

.. code-block:: javascript

   hercules.http.post(url, content, options) 

This function executes HTTP POST request for the given URL.

- *url* - HTTP request URL
- *content* - request content (see hercules.http.request for details)
- *options* - optional options object (see hercules.http.request for details)


**Example:** Use yandex translation to translate a text.

.. code-block:: javascript

    const content = {texts: [sourceText], sourceLanguageCode: 'en', targetLanguageCode: 'ru'};
    const result = hercules.http.post('POST', 'https://translate.api.cloud.yandex.net/translate/v2/translate', content, {apiKey: apiKey});
    const translatedText = result.translations[0].text;

put
----------------

.. code-block:: javascript

   hercules.http.put(url, content, options) 

This function executes HTTP PUT request for the given URL.

- *url* - HTTP request URL
- *content* - request content (see hercules.http.request for details)
- *options* - optional options object (see hercules.http.request for details)

request
----------------

.. code-block:: javascript

   hercules.http.request(method, url, content, options) 

This function executes HTTP PUT request for the given URL.

- *method* - HTTP method, e.g. ``'GET'`` or ``'POST'``
- *url* - HTTP request URL
- *content* - request content (string or JSON)
- *options* - optional options object

Allowed options are:

- *apiKey* - API key for **Api-Key** authentication scheme
- *username* - user name for **Basic** authentication scheme
- *password* - user password for **Basic** authentication scheme
- *contentType* - request content type, ``application/json`` by default if content is provided
