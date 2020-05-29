StepHeigth
==========
A standalone command line tool to evaluate surface topography files of step-height or groove standards for the required parameter (the depth or the height). Surface data files are provided as BCR files with .sdf file name extension. This file format is defined in ISO 25178-7, ISO 25178-71 and EUNA 15178 and is very common in the surface texture comunity.

Subject of this analysis are depth setting standards as defined in ISO 5436-1. The relevant features are rectangular (type A1) or cylindrical (type A2) grooves or ridges relative to the nominally flat reference part. The actual evaluation is performed on a per profile basis. The profiles are not referenced to each other, i.e. the fit is purly on separated 2D lines.

The surface data provided by the input file is used as such, no leveling or spatial filtering is performed by this tool. If needed, processing must take place prior to writing the BCR file.

## Defining Feature

### Feature Type
The kind of feature must be provided by the `--type (-t)` command line option.

### Location
The location is set by the positions of its two edges (for steps a single edge only). The edge positions must be defined by the `--X1` and `--X2` command line options.

Trapezoid features...

Vertical features... 

Edges paralell to the y axis of the scan field.

### Fit Region
The locations of the fit regions (A, B, C according to ISO 5436-1) are defined relative to the edge positions. The distance between the edges (the feature width *W*) defines the unit scale for the  parameters *W*1, *W*2 and *W*3. Due to the definition of the three parameters the fit regions are symmetric to the feature edges.

## Command Line Usage  

```
StepHeight inputfile [outputfile] [plotfile] [options]
```

## Options

`--multifile` : Use three separate input files.

`--X1` : x-value of first feature edge, in µm.

`--X2` : x-value of second feature edge, in µm.

`--W1` : Parameter *W*1 of evaluation region (default 3).

`--W2` : Parameter *W*2 of evaluation region (default 2/3).

`--W3` : Parameter *W*3 of evaluation region (default 1/3).

`--Y0` : y coordinate value of first profile, in µm.

`--Ywidth` : Width of y band of profiles which should be considered, in µm

`--maxspan` : Discard fit if range of residuals exceed this value for a given profile, in µm.

`--outextension` : File name extension for output file.

`--resextension` : File name extension for residual plot file.

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



