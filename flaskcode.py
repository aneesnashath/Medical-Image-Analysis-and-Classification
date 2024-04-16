import numpy as np
import cv2
import os
import requests  # Add this import statement
from sklearn.model_selection import train_test_split
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Flatten, Dense
from tensorflow.keras.optimizers import Adam
from PIL import Image
from werkzeug.utils import secure_filename
from flask import Flask, request, jsonify

# Define global variables
IMG_WIDTH = 224
IMG_HEIGHT = 224

app = Flask(__name__)
app.config['MAX_CONTENT_LENGTH'] = 32 * 1024 * 1024  # Set maximum request size to 32 MB
UPLOAD_FOLDER = 'uploads'  # Define the upload folder
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER

# Define the function to load PneumoniaMNIST data and format labels for binary classification
def load_pneumoniamnist_data():
    # Define the URL to download the PneumoniaMNIST dataset
    url = 'https://zenodo.org/record/4269852/files/pneumoniamnist.npz?download=1'

    # Define the output folder
    output_folder = 'data_pneumonia'

    # Create the output folder if it doesn't exist
    os.makedirs(output_folder, exist_ok=True)

    # Download the dataset
    r = requests.get(url, allow_redirects=True)

    # Load the .npz file
    npz_file = np.load(os.path.join(output_folder, 'pneumoniamnist.npz'), allow_pickle=True)

    # Extract the images and labels from the .npz file
    images = npz_file['train_images']
    labels = npz_file['train_labels']

    # Normalize the images to values between 0 and 1
    images = images.astype('float32') / 255.0

    # Split the data into train and test sets
    train_images, test_images, train_labels, test_labels = train_test_split(
        images, labels, test_size=0.2, random_state=42)

    # Convert labels to binary format (0 or 1)
    train_labels = (train_labels == 1).astype(int)
    test_labels = (test_labels == 1).astype(int)

    return train_images, train_labels, test_images, test_labels

# Load PneumoniaMNIST data
train_images_pneumoniamnist, train_labels_pneumoniamnist, test_images_pneumoniamnist, test_labels_pneumoniamnist = load_pneumoniamnist_data()

# Convert PneumoniaMNIST images to RGB format
def load_and_preprocess_images(images):
    processed_images = []
    for img in images:
        img_array = cv2.resize(img, (IMG_WIDTH, IMG_HEIGHT))
        img_array = cv2.cvtColor(img_array, cv2.COLOR_GRAY2RGB)  # Convert grayscale to RGB
        img_array = img_array / 255.0  # Normalize pixel values to [0, 1]
        processed_images.append(img_array)
    return np.array(processed_images)

train_images_pneumoniamnist_rgb = load_and_preprocess_images(train_images_pneumoniamnist)
test_images_pneumoniamnist_rgb = load_and_preprocess_images(test_images_pneumoniamnist)

# Create the classification model
model_pneumoniamnist = Sequential([
    Flatten(input_shape=(IMG_WIDTH, IMG_HEIGHT, 3)),  
    Dense(128, activation='relu'),
    Dense(1, activation='sigmoid')
])

# Compile the model
model_pneumoniamnist.compile(optimizer='adam',
                             loss='binary_crossentropy',
                             metrics=['accuracy'])

# Train the model
model_pneumoniamnist.fit(train_images_pneumoniamnist_rgb, train_labels_pneumoniamnist, epochs=10, batch_size=32, validation_data=(test_images_pneumoniamnist_rgb, test_labels_pneumoniamnist))

# Define route for image classification
@app.route('/classify', methods=['POST'])
def classify_image():
    if 'image' not in request.files:
        return jsonify({'error': 'No image uploaded'})

    image_file = request.files['image']
    if image_file.filename == '':
        return jsonify({'error': 'No image selected'})

    # Preprocess the uploaded image
    image = Image.open(image_file)
    resized_image = image.resize((IMG_WIDTH, IMG_HEIGHT))
    image_array = np.array(resized_image)
    image_array_normalized = image_array.astype('float32') / 255.0
    final_image = np.expand_dims(image_array_normalized, axis=0)

    # Make prediction
    prediction = model_pneumoniamnist.predict(final_image)
    predicted_class = "Pneumonia" if prediction > 0.1 else "Normal"

    # Log the request and prediction
    app.logger.info(f"Image uploaded: {image_file.filename}, Prediction: {predicted_class}")

    return jsonify({'prediction': predicted_class})

# Define a route to handle image uploads from Unity
@app.route('/upload', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return jsonify({'error': 'No image uploaded'})

    image_file = request.files['image']
    if image_file.filename == '':
        return jsonify({'error': 'No image selected'})

    try:
        # Save the uploaded image
        filename = secure_filename(image_file.filename)
        filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        image_file.save(filepath)

        # Debug statement to check if the file is saved successfully
        app.logger.info(f"Image saved at: {filepath}")

        return jsonify({'message': 'Image uploaded successfully', 'filename': filename})
    except Exception as e:
        return jsonify({'error': str(e)})

if __name__ == '__main__':
    app.run(debug=True)