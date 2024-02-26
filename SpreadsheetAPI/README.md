"# SpreadSheetApi" 

Some work assumptions

* should be paired with an instance of mongo ("ConnectionString": "mongodb://localhost:27017",)

* columns can be of types: string, int, boolean - to enter a string prefix the value with $ - eg. "$value", to enter a non-string value just enter the value as a string - eg. "123"

* for convenience it is assumed columns have simple char-only unique names

* any lookup must follow the given structure of: "lookup("name",index) - eg. ("lookup,5")

* it is assumed each column can have up to 10 rows, that number is easily tweakable however its hard coded in this case for simplicity. a prettier way would be to make it configurable in the settings or by user request however i'm assuming this task isn't here to test that

* for testing simplicity and to avoid cluttering project the the controller is in charge of both validations and mission-logic. 

