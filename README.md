StepHeigth
==========
A standalone command line tool to evaluate surface topography files of step-height or groove standards for the required parameter. Surface data files must be provided according to ISO 25178-7, ISO 25178-71 and EUNA 15178 (BCR).

## Command Line Usage:  

```
StepHeight inputfile [outputfile] [plotfile] [options]
```

## Options:  

`--multifile` : Use three separate input files.

`--X1` : x-value of first feature edge, in µm.

`--X2` : x-value of second feature edge, in µm.

`--W1` : Parameter W1 of evaluation region (default 3).

`--W2` : Parameter W2 of evaluation region (default 2/3).

`--W3` : Parameter W3 of evaluation region (default 1/3).

`--Y0` : y-value of first profile, in µm.

`--Ywidth` : Width of y band to evaluate, in µm

`--maxspan` : Discard fit if residuals are larger than this value, in µm.

`--outextension` : Extension for output file.

`--resextension` : Extension for residual file.

`--quiet (-q)` : Quiet mode. No console output (except for errors).

`--comment` : User supplied string to be included in the output file.

`--type (-t)` : Feature type to be fitted, supported values are:

1: ISO A1 (rectangular ridge)

2: ISO A2 (cylindrical groove)

3: ISO A1 (rectangular groove)

4: ISO A2 (cylindrical ridge)

5: rising step

6: falling step

## Dimensional Parameters
W1 W2 W3
X1, X2
Coordinate origin

## Multiple File Input
It may be necessary to divide the scan field to separate files. Some scan techniques do not work with very steep flanks, so the bottom and the reference surface must be recorded separately. By using the `--multifile` option it is possible to handle this case also. 

## Dependencies  
Bev.SurfaceRasterData:  https://github.com/matusm/Bev.SurfaceRasterData  

Bev.IO.BcrReader: https://github.com/matusm/Bev.IO.BcrReader 

CommandLineParser: https://github.com/commandlineparser/commandline 



