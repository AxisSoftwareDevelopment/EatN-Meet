import requests

API_KEY = "AIzaSyCMtGUFQina5iFjObHDN2GBhp6S_Xuwd8c"
GOOGLE_PLACES_URL = f"https://places.googleapis.com/v1/places"

class MockRequest:
    def __init__(self, data):
        self.data = data

request = MockRequest({"textQuery": "Spicy Vegetarian Food in Sydney, Australia"})

def MAPS_GET_RESTAURANTS():
    print("\n\t<----- Started Cloud Function - MAPS_GET_RESTAURANTS ----->")
    try:
        param_TextQurery = request.data["textQuery"] # Get the text query from the request data
        url = GOOGLE_PLACES_URL + ":searchText" # Construct the URL for the Places API search endpoint
        # Set the headers for the request, including the API key and content type
        headers={
            "X-Goog-Api-Key": API_KEY,
            "Content-Type": "application/json",
            "X-Goog-FieldMask": "places.id,places.displayName,places.formattedAddress,nextPageToken "
        }
        data = {"textQuery": param_TextQurery} # Create the data payload with the text query

        # Make the GET request to the Places API
        response = requests.post(url, headers=headers, json=data)
        if response.status_code == 200:
            print("MAPS_GET_RESTAURANTS - Request successful.")
            json_data = response.json()
            next_page_token = json_data.get("nextPageToken")
            print(json_data)
            places = json_data.get("places", [])
            if not places:
                places = []
            print(f"MAPS_GET_RESTAURANTS - Found {len(places)} places.")
            #return https_fn.Response(places, status=200)
        else:
            print(f"MAPS_GET_RESTAURANTS - Request failed with status code: {response.status_code}")
            #return https_fn.Response({"error": "Failed to fetch places"}, status=response.status_code)
    except Exception as ex:
        print("FB Function Failed - MAPS_GET_RESTAURANTS ->" + str(ex))
    print("\n\t<----- Finished Cloud Function - MAPS_GET_RESTAURANTS ----->")

MAPS_GET_RESTAURANTS()  # Call the function to execute it