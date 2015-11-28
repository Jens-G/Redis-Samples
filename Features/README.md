# Feature Showcase Samples

Demostrates various features, such as

- Setting and working with simple fields: strings, integers, floats
- Hashes in comparison to normal fields
- Finding primes using Redis bitmaps and bit operations
- Publish/Subscribe
- Working with sets and sorted sets, and utilizing set operations on them
- Transactions and locks

## Remarks

For some samples you need additional data and may have to adjust the file paths in the source. In particular, these data are needed:

### "Sets" example 

Requires ```famousbirthdays.csv```, which is expected to have a format similar to the data  of the famousbirthdays.sql file from http://mydatamaster.com/free-downloads. This PHP dump SQL file needs to be manually converted into CSV. The editor Notepad++ is recommended for this task, it basically takes only 5 minutes of search and replace (if you do it right).

### "Sorted Sets" example 

Requires the [airports.csv](http://ourairports.com/data/airports.csv) file from [OurAirports.com](http://ourairports.com). 

