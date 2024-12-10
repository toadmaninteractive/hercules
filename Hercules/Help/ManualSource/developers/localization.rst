.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

*********************
Localization
*********************

Schema Example
-------------------------

.. code-block:: igor

    enum Locale
    {
        en = 0;
        de;
        fr;
        ...
    }
    
    record LocalizedEntry
    {
        string original;
        string translation;
        ?string timestamp;     // Not required, but used by Thalia if present
        ?string user;          // Not required, but used by Thalia if present
        ...
    }
    
    [schema editor=localized]
    record LocalizedString
    {
        string text;
        string approved_text;    // Not required, but used for text approval if present
        dict<Locale, LocalizedEntry> data;  // Used by Thalia
        ... 
    }

LocalizedString defined this way with ``[schema editor=localized]`` attribute is recognized by Hercules. 

Text Approval
----------------------

It is often that when localized texts are added to Hercules, they are first filled with some temporary values or placeholders. 
There may be a dedicated person or a team that is responsible for editing and approving all native texts in game.

If ``aproved_text`` field is present in LocalizedString, Hercules will provide text approval functionality.

``approved_text`` stores the last value of ``text`` that has been approved by the editor. 
If ``text`` changes, ``approved_text`` becomes outdated and requires new approval (which is performed by clicking the red flag button).

If ``text`` is the same as ``approved_text``, the text is considered approved.


