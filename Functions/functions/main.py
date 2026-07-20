# Welcome to Cloud Functions for Firebase for Python!
# To get started, simply uncomment the below code or create your own.
# Deploy with `firebase deploy`

import json
import time
from firebase_functions import https_fn
from firebase_functions import params
from firebase_admin import initialize_app, messaging
import requests

GOOGLE_PLACES_API_KEY = params.SecretParam("GOOGLE_PLACES_API_KEY")
GOOGLE_PLACES_URL = f"https://places.googleapis.com/v1/"
HTTP_TIMEOUT_SECONDS = (3.05, 10)
HTTP_RETRY_COUNT = 2
RETRY_STATUS_CODES = {429, 500, 502, 503, 504}
MAX_PAGE_SIZE = 20
MAX_SEARCH_RADIUS_METERS = 50000
MIN_IMAGE_DIMENSION = 1
MAX_IMAGE_DIMENSION = 1600

HTTP_SESSION = requests.Session()

app = initialize_app()


def _get_google_places_api_key() -> str:
    api_key = GOOGLE_PLACES_API_KEY.value.strip()
    if not api_key:
        raise ValueError("GOOGLE_PLACES_API_KEY is not configured.")
    if not api_key.startswith("AIza"):
        raise ValueError("GOOGLE_PLACES_API_KEY appears invalid (unexpected format).")
    return api_key


def _require_authenticated_user(request: https_fn.CallableRequest) -> str:
    auth = getattr(request, "auth", None)
    uid = getattr(auth, "uid", None) if auth else None
    if not uid:
        raise https_fn.HttpsError(
            code=https_fn.FunctionsErrorCode.UNAUTHENTICATED,
            message="Authentication required.",
        )
    return uid


def _build_response(output: object, error: str = "", next_page_token: str | None = None) -> str:
    payload: dict[str, object] = {
        "output": output,
        "errors": error,
    }
    if next_page_token is not None:
        payload["nextPageToken"] = next_page_token
    return json.dumps(payload)


def _clamp(value: int, minimum: int, maximum: int) -> int:
    return max(minimum, min(value, maximum))


def _as_number(value, default=0):
    """Coerce a value to a number (int) safely.

    Handles ints, floats, numeric strings, and simple dict wrappers like {'value': 123} which
    some clients/platforms may send when serializing.
    """
    if value is None:
        return default
    # Already numeric
    if isinstance(value, (int, float)):
        return value
    # Numeric string
    if isinstance(value, str):
        try:
            if "." in value:
                return float(value)
            return int(value)
        except Exception:
            return default
    # Wrapped in a dict (common from some platforms)
    if isinstance(value, dict):
        # Try common wrapper keys
        for k in ("value", "Value", "v", "V"):
            if k in value:
                return _as_number(value[k], default)
        # If dict contains a single item, try to coerce it
        if len(value) == 1:
            try:
                only = next(iter(value.values()))
                return _as_number(only, default)
            except Exception:
                return default
        return default
    return default


def _http_request(method: str, url: str, **kwargs):
    last_error: Exception | None = None
    for attempt in range(HTTP_RETRY_COUNT + 1):
        try:
            response = HTTP_SESSION.request(method, url, timeout=HTTP_TIMEOUT_SECONDS, **kwargs)
            if response.status_code in RETRY_STATUS_CODES and attempt < HTTP_RETRY_COUNT:
                time.sleep(0.4 * (2 ** attempt))
                continue
            return response
        except requests.RequestException as ex:
            last_error = ex
            if attempt < HTTP_RETRY_COUNT:
                time.sleep(0.4 * (2 ** attempt))
                continue
            raise

    if last_error is not None:
        raise last_error
    raise RuntimeError("Unexpected HTTP request failure")

@https_fn.on_call()
def TRIGGER_NOTIFICATION(request: https_fn.CallableRequest):
    METHOD_NAME = "TRIGGER_NOTIFICATION"
    print(f"\n\t<----- Starting Cloud Function - {METHOD_NAME} ----->")
    try:
        _require_authenticated_user(request)
        param_Type = request.data["type"]
        param_Topic = request.data["topic"]
        param_FCMTokens = request.data["fcm_tokens"]
        param_Title = request.data["title"]
        param_Body = request.data["body"]
        print(f"{METHOD_NAME} - Type: [{param_Type}] -> Topic: {param_Topic} -> TokenCount: {len(param_FCMTokens)}")

        notification = messaging.Notification (
            title=param_Title,
            body=param_Body)
            #image="" )

        msgs = []

        if param_Type == "TOKENS":
            if len(param_FCMTokens) < 1:
                print(f"{METHOD_NAME} - No Tokens passed.")
                return
            print(f"{METHOD_NAME} - There are [{len(param_FCMTokens)}] tokens to send notifications to.")
            msgs = [ messaging.Message(token=token, notification=notification) for token in param_FCMTokens ]
        else:
            msgs = [ messaging.Message(topic=param_Topic, notification=notification) ]

        batch_response: messaging.BatchResponse = messaging.send_each(msgs)

        if batch_response.failure_count < 1:
            print(f"{METHOD_NAME} - Messages sent successfully")
            return {"success": True, "failureCount": 0}
        else:
            print(f"{METHOD_NAME} - [{str(batch_response.failure_count)}] messages failed.")
            return {"success": False, "failureCount": batch_response.failure_count}
    except https_fn.HttpsError:
        raise
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
        raise https_fn.HttpsError(
            code=https_fn.FunctionsErrorCode.INTERNAL,
            message="Failed to trigger notifications.",
            details=str(ex),
        )
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

@https_fn.on_call(secrets=[GOOGLE_PLACES_API_KEY])
def MAPS_GET_ALL_RESTAURANTS(request: https_fn.CallableRequest):
    METHOD_NAME = "MAPS_GET_ALL_RESTAURANTS"
    print(f"\n\t<----- Started Cloud Function - {METHOD_NAME} ----->")
    try:
        _require_authenticated_user(request)
        param_TextQuery = request.data.get("textQuery", "") # Get the text query from the request data
        param_Location_Latitude = request.data.get("locationLatitude", 0.0) # Get the latitude from the request data, default to 0.0
        param_Location_Longitude = request.data.get("locationLongitude", 0.0) # Get the longitude from the request data, default to 0.0
        # Coerce numeric params safely — some clients may serialize numbers as wrapped dicts
        param_SearchRadius = _as_number(request.data.get("searchRadius", -1), -1) # Get the search radius from the request data, default to -1 (no radius)
        param_PageSize = _as_number(request.data.get("pageSize", 10), 10) # Get the page size from the request data, default to 10
        param_PageToken = request.data.get("pageToken", "") # Get the page token from the request data, default to empty string
        url = GOOGLE_PLACES_URL + "places:searchText" # Construct the URL for the Places API search endpoint

        if param_SearchRadius < 250:
            param_SearchRadius = 250 # Ensure the search radius is at least 250 meters
        param_SearchRadius = _clamp(int(param_SearchRadius), 250, MAX_SEARCH_RADIUS_METERS)
        param_PageSize = _clamp(int(param_PageSize), 1, MAX_PAGE_SIZE)

        api_key = _get_google_places_api_key()

        # Set the headers for the request, including the API key and content type
        headers={
            "X-Goog-Api-Key": api_key,
            "Content-Type": "application/json",
            "X-Goog-FieldMask": "places.id,places.displayName,places.formattedAddress,places.location,places.photos,nextPageToken"
        }
        data = {"textQuery": param_TextQuery, "pageSize": param_PageSize,}# "rankPreference": "DISTANCE"} # Create the data payload with the text query and page size
        # Add the location bias. This is necessary as no location bias causes weird results.
        data["locationBias"] = {
            "circle": {
                "center": {
                    "latitude": param_Location_Latitude,
                    "longitude": param_Location_Longitude
                },
                "radius": param_SearchRadius
            }
        }
        # Add the page token to the data payload if provided
        if len(param_PageToken) > 0:
            data["pageToken"] = param_PageToken

        # Make the GET request to the Places API
        response = _http_request("POST", url, headers=headers, json=data)
        if response.status_code == 200:
            print(f"{METHOD_NAME} - Request successful.")
            output = FormatJSONResponse_TextSearch(response.text.strip(), "")
        else:
            print(f"{METHOD_NAME} - Request failed with status code: [{response.status_code}]")
            output = FormatJSONResponse_TextSearch("", response.text.strip())
        print(f"{METHOD_NAME} - Output: [{output}]")
        print(f"{METHOD_NAME} - At URL: [{url}]")
        print(f"{METHOD_NAME} - With headers: [{{'X-Goog-Api-Key': '<redacted>', 'Content-Type': 'application/json', 'X-Goog-FieldMask': 'places.id,places.displayName,places.formattedAddress,places.location,places.photos,nextPageToken'}}]")
        print(f"{METHOD_NAME} - With data: [{data}]")
        print(f"{METHOD_NAME} - Upstream response status: [{response.status_code}]")
        print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")
        return output
    except https_fn.HttpsError:
        raise
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
        return _build_response([], str(ex), "")
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

@https_fn.on_call(secrets=[GOOGLE_PLACES_API_KEY])
def MAPS_GET_PLACE_DETAILS(request: https_fn.CallableRequest):
    METHOD_NAME = "MAPS_GET_PLACE_DETAILS"
    print(f"\n\t<----- Started Cloud Function - {METHOD_NAME} ----->")
    try:
        _require_authenticated_user(request)
        param_PlaceId = request.data.get("placeID", "") # Get the place ID from the request data
        url = GOOGLE_PLACES_URL + "places/" + param_PlaceId # Construct the URL for the Places API search endpoint

        api_key = _get_google_places_api_key()

        # Set the headers for the request, including the API key and content type
        headers={
            "X-Goog-Api-Key": api_key,
            "Content-Type": "application/json",
            "X-Goog-FieldMask": "id,displayName,formattedAddress,location,photos"
        }

        # Make the GET request to the Places API
        response = _http_request("GET", url, headers=headers)
        if response.status_code == 200:
            print(f"{METHOD_NAME} - Request successful.")
            output = FormatJSONResponse_PlaceDetails(response.text.strip(), "")
        else:
            print(f"{METHOD_NAME} - Request failed with status code: [{response.status_code}]")
            output = FormatJSONResponse_PlaceDetails("", response.text.strip())
        print(f"{METHOD_NAME} - Output: [{output}]")
        print(f"{METHOD_NAME} - At URL: [{url}]")
        print(f"{METHOD_NAME} - With headers: [{{'X-Goog-Api-Key': '<redacted>', 'Content-Type': 'application/json', 'X-Goog-FieldMask': 'id,displayName,formattedAddress,location,photos'}}]")
        print(f"{METHOD_NAME} - Upstream response status: [{response.status_code}]")
        print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")
        return output
    except https_fn.HttpsError:
        raise
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
        return _build_response({}, str(ex), None)
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

@https_fn.on_call(secrets=[GOOGLE_PLACES_API_KEY])
def MAPS_GET_PLACE_PICTURES(request: https_fn.CallableRequest):
    METHOD_NAME = "MAPS_GET_PLACE_PICTURES"
    print(f"\n\t<----- Started Cloud Function - {METHOD_NAME} ----->")
    try:
        _require_authenticated_user(request)
        param_PhotoNames = request.data.get("photoNames", "") # Get the photo names from the request data
        param_MaxHeightPx = _clamp(int(_as_number(request.data.get("maxHeightPx", 400), 400)), MIN_IMAGE_DIMENSION, MAX_IMAGE_DIMENSION)
        param_MaxWidthPx = _clamp(int(_as_number(request.data.get("maxWidthPx", 400), 400)), MIN_IMAGE_DIMENSION, MAX_IMAGE_DIMENSION)
        photos = GetPlacePictures(METHOD_NAME, param_PhotoNames, param_MaxWidthPx, param_MaxHeightPx) # Call the function to get the place pictures
        print(f"{METHOD_NAME} - Output: [{photos}]")
        print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")
        return photos
    except https_fn.HttpsError:
        raise
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
        return ""
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

def FormatJSONResponse_TextSearch(response: str, error: str) -> str:
    try:
        response_json = json.loads(response)

        # Set Profile Picture for each place
        for place in response_json.get("places", []):
            if "photos" in place and len(place["photos"]) > 0:
                photo_name = place["photos"][0]["name"]
                photo = GetPlacePictures("FormatJSONResponse_TextSearch", photo_name)
                place["profilePicture"] = photo
            else:
                place["profilePicture"] = ""

        # Convert the modified response back to a JSON string
        output_value = response_json.get("places", [])
        nextPageToken = response_json.get("nextPageToken", "")
    except Exception:
        output_value = []
        nextPageToken = ""

    return _build_response(output_value, error, nextPageToken)

def FormatJSONResponse_PlaceDetails(response: str, error: str) -> str:
    try:
        response_json = json.loads(response)

        # Set Profile Picture for each place
        if "photos" in response_json and len(response_json["photos"]) > 0:
            photo_name = response_json["photos"][0]["name"]
            photo = GetPlacePictures("FormatJSONResponse_PlaceDetails", photo_name)
            response_json["profilePicture"] = photo
        else:
            response_json["profilePicture"] = ""

        # Convert the modified response back to a JSON string
        output_value = response_json
    except Exception:
        output_value = {}

    return _build_response(output_value, error, None)

def GetPlacePictures(methodName: str, photo_names: str, max_width: int = 400, max_height: int = 400) -> str:
    api_key = _get_google_places_api_key()
    photos = ""
    for photo_name in photo_names.split(","):
        # Construct the URL for the Places API Place Photos endpoint
        url = GOOGLE_PLACES_URL + f"{photo_name}/media?key={api_key}&maxHeightPx={max_height}&maxWidthPx={max_width}&skipHttpRedirect=true"
        print(f"{methodName} - Requesting photo for: [{photo_name}]")

        # Make the GET request to the Places API
        response = _http_request("GET", url)

        if response.status_code == 200:
            print(f"{methodName} - Photo request successful.")
            if(len(photos) > 0):
                photos += ","
            response_json = response.json()
            photos += response_json.get("photoUri", "")
        else:
            print(f"{methodName} - Photo request failed with status code: [{response.status_code}]")
    return photos