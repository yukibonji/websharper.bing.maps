﻿namespace WebSharper.Bing.Maps

open WebSharper
open WebSharper.JavaScript

module Rest =

    [<JavaScript>]
    let private credentials = "Ai6uQaKEyZbUvd33y5HU41hvoov_piUMn6t78Qzg7L1DWY4MFZqhjZdgEmCpQlbe"

    [<JavaScript>]
    let private restApiUri = "http://dev.virtualearth.net/REST/v1/"

    [<JavaScript>]
    let private IsUndefined x =
        JS.TypeOf x = JS.Kind.Undefined

    [<JavaScript>]
    let private SendRequest req =
        let script = JS.Document.CreateElement("script")
        script.SetAttribute("type", "text/javascript")
        script.SetAttribute("src", req)
        JS.Document.DocumentElement.AppendChild script |> ignore

    [<JavaScript>]
    let private RequestCallbackName = "BingOnReceive"

    [<JavaScript>]
    let private RequestStringBoilerplate credentials = "output=json&jsonp=" + RequestCallbackName + "&key=" + credentials

    [<JavaScript>]
    let private OptionalFields request arr =
        arr
        |> Array.choose (fun name ->
            let value = (?) request name
            if IsUndefined value then None else
                Some (name + "=" + string value))

    [<JavaScript>]
    let RequestLocationByAddress(credentials, address : Address, callback : RestResponse -> unit) =
        (?<-) JS.Global RequestCallbackName callback
        let fields =
            OptionalFields address
                [|"adminDistrict"; "locality"; "addressLine"; "countryRegion"; "postalCode"|]
        let req = String.concat "&" fields
        let fullReq = restApiUri + "Locations?" + req + "&" + RequestStringBoilerplate credentials
        SendRequest fullReq

    [<JavaScript>]
    let RequestLocationByQuery(credentials, query : string, callback : RestResponse -> unit) =
        (?<-) JS.Global RequestCallbackName callback
        let req = restApiUri + "Locations?query=" + query + "&" + RequestStringBoilerplate credentials
        SendRequest req

    [<JavaScript>]
    let RequestLocationByPoint(credentials, x:float, y:float, entities, callback : RestResponse -> unit) =
        (?<-) JS.Global RequestCallbackName callback
        let retrieveEntities = function
        | [] -> ""
        | l -> "&includeEntityTypes=" + String.concat "," l
        let req =
            restApiUri + "Locations/" + string x + "," + string y +
                "?" + RequestStringBoilerplate credentials +
                retrieveEntities entities
        SendRequest req

    [<JavaScript>]
    let private StringifyWaypoints waypoints =
        Array.mapi (fun i (w:Waypoint) -> "wp." + string i + "=" + string w) waypoints

    [<JavaScript>]
    let RequestRoute(credentials, request : RouteRequest, callback : RestResponse -> unit) =
        (?<-) JS.Global RequestCallbackName callback
        let fields =
            OptionalFields request
                [| "avoid"; "heading"; "optimize"; "routePathOutput"; "distanceUnit"
                   "dateTime"; "timeType"; "maxSolutions"; "travelMode" |]
            |> Array.append (StringifyWaypoints request.Waypoints)
        let req = String.concat "&" fields
        let fullReq = restApiUri + "/Routes?" + req + "&" + RequestStringBoilerplate credentials
        SendRequest fullReq

    [<JavaScript>]
    let RequestRouteFromMajorRoads(credentials, request : RouteFromMajorRoadsRequest, callback : RestResponse -> unit) =
        (?<-) JS.Global RequestCallbackName callback
        let fields =
            OptionalFields request
                [| "destination"; "exclude"; "routePathOutput"; "distanceUnit" |]
        let req = String.concat "&" fields
        let fullReq = restApiUri + "/Routes/FromMajorRoads?" + req + "&" + RequestStringBoilerplate credentials
        SendRequest fullReq


    [<JavaScript>]
    let RequestImageryMetadata(credentials, request : ImageryMetadataRequest,
                               callback : RestResponse -> unit) =
        (?<-) JS.Global RequestCallbackName callback
        let fields =
            OptionalFields request
                [| "include"; "mapVersion"; "orientation"; "zoomLevel" |]
        let req = String.concat "&" fields
        let fullReq =
            restApiUri + "Imagery/Metadata/" + string request.ImagerySet +
            (if not(IsUndefined request.CenterPoint) then "/" + request.CenterPoint.ToUrlString() else "") + "?" +
            req + "&" + RequestStringBoilerplate credentials
        SendRequest fullReq

    [<JavaScript>]
    let StaticMapUrl(credentials, request : StaticMapRequest) =
        let fields =
            [
                yield! OptionalFields request
                    [| "avoid"; "dateTime"; "mapLayer"; "mapVersion"
                       "maxSolutions"; "optimize"; "timeType"; "travelMode"; "zoomLevel" |]
                if not (IsUndefined request.MapArea) then
                    yield (fst request.MapArea).ToUrlString() + "," + (snd request.MapArea).ToUrlString()
                if not (IsUndefined request.MapSize) then
                    yield string (fst request.MapSize) + "," + string (snd request.MapSize)
                if not (IsUndefined request.Pushpin) then
                    let pushpinToUrlString (pin : PushpinRequest) =
                        let coords = string pin.X + "," + string pin.Y
                        let icstyle = if IsUndefined pin.IconStyle then "" else string pin.IconStyle
                        let label = if IsUndefined pin.Label then "" else pin.Label
                        coords + ";" + icstyle + ";" + label
                    yield! request.Pushpin |> Array.map (fun pin -> "pp=" + pushpinToUrlString pin)
                if not (IsUndefined request.Waypoints) then
                    yield! StringifyWaypoints request.Waypoints
                if not (IsUndefined request.DeclutterPins) then
                    yield "dcl=" + if request.DeclutterPins then "1" else "0"
                if not (IsUndefined request.DistanceBeforeFirstTurn) then
                    yield "dbft=" + string request.DistanceBeforeFirstTurn
            ]
        let query =
            if not (IsUndefined request.Query) then
                request.Query
            elif not (IsUndefined request.CenterPoint) then
                request.CenterPoint.ToUrlString() + "/" + string request.ZoomLevel
            else
                ""
        let hasRoute = not (IsUndefined request.Waypoints)
        let req = String.concat "&" fields
        let fullReq =
            restApiUri + "Imagery/Map/" +
            string request.ImagerySet + "/" +
            (if hasRoute then "Route/" else "") + query + "?" +
            req + "&key=" + credentials
        fullReq

    [<JavaScript>]
    let StaticMap(credentials, request) =
        let img = JS.Document.CreateElement("img")
        img.SetAttribute("src", StaticMapUrl(credentials, request))
        img
