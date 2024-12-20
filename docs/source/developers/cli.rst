.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

******************************
Command Line Interface
******************************

Batch Mode
-----------

Batch mode allows to execute Hercules scripts as a part of continuous integration pipeline. Hercules starts, executes the script and exits immediately.

Hercules returns 0 as a success exit code and 1 as an error exit code.

*Example:* 

.. code-block:: batch

   Hercules.exe -batch https://host/scriptname -log %WORKSPACE%\hercules.log


Command Line Options
--------------------

* ``-reset_layout`` - resets window layout to default
* ``-log LOGFILE`` - saves log to **LOGFILE**
* ``-batch SCRIPT`` - runs **SCRIPT** and exits. **SCRIPT** is the filename path or Hercules URL, in which case the database connection is loaded first
* ``-open HERCULESURL`` - opens **HERCULESURL** on program start
* ``-dispatch HERCULESURL`` - opens **HERCULESURL** in existing (if possible) or new application instance

If Hercules is run with a single argument that doesn't start with ``-``, it is considered to be a **HERCULESURL**, and Hercules opens this URL on load.

