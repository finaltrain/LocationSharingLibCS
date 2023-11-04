# LocationSharingLibCS
LocationSharingLibCS is a C# wrapper for LocationSharingLib coded by python.
It was intended to be a wrapper, but it has been modified so much that it is no longer a wrapper.

# Usage
*You need to prepare the cookie file in the same way as the original locationsharinglib in advance.*

First, you need to install package Newtonsoft.Json.

In the constructor of LocationSharingLibCS, pass the path to the cookie file (absolute or relative path) as string or the contents of the cookie file as StreamReader.
At the same time, language and countryCode can be specified to set the language and region. If not specified, English and U.S. will be used.

The data can then be retrieved using Get methods and updated to the latest data using Update methods.
In order to get the latest data, it is necessary to execute the Update method first, not just the Get method.
For example, if you want to get the location information of yourself (the user who got the cookie), you can execute GetCordinatesOfAuthenticatedPerson() after UpdateAuthenticatedPerson(), which will return the cordinate are returned as string tuple.

# Author
finaltrain(旅するデジニンジャ)
* X:https://x.com/final_train

# Licence
Original https://github.com/costastf/locationsharinglib
The original library is MIT License. This library is also MIT Licence.
