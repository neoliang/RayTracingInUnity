#I "/Users/neo/.nuget/packages/"
#r "glmnet/0.7.0/lib/net40/GlmNet.dll"
#r "xplot.plotly/2.0.0/lib/net472/XPlot.Plotly.dll"
#r "newtonsoft.json/12.0.1/lib/net40/Newtonsoft.Json.dll"
#r "/Users/neo/Projects/RayTracing/RT/bin/Release/netcoreapp2.2/RT.dll"
open System
open XPlot.Plotly
open RT1
open GlmNet

let pdf p = 1.0 / (4.0 * Math.PI)
let randvec3 () = Exten.RandomVecInSphere() |> glm.normalize
let rd = Random()
let randRange a b = rd.NextDouble() * ( b - a) + a
let main n = [for i in 1..n -> randvec3()] |> List.map(fun v -> float(v.z * v.z)/(pdf v)) |> List.average 

let mci f a b pdf n = 
	[for i in 1..n -> (randRange a b)]
	|> List.map (fun k -> (f k) / (pdf k))
	|> List.average 