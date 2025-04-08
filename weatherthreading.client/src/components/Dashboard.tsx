import { LineChart, XAxis, YAxis, Line, Tooltip, CartesianGrid, ResponsiveContainer } from "recharts";
import React, { useState } from "react";
import "../index.css";

const cities = [
  "Budapest, Hungary", "Bangkok, Thailand", "Zurich, Switzerland", "Los Angeles, USA", "Halifax, Canada", 
  "Rome, Italy", "London, UK", "Riga, Latvia", "Barcelona, Spain", "Shanghai, China", 
  "Tokyo, Japan", "Paris, France", "Stockholm, Sweden", "Munich, Germany", "Riyadh, Saudi Arabia"
];

const yAxisOptions = [
  { label: "Temperature", value: "temperature" },
  { label: "Relative Humidity", value: "relative_humidity_2m" },
  { label: "Precipitation Sum", value: "precipitation_sum" },
  { label: "Precipitation Hours", value: "precipitation_hours" },
  { label: "Wind Speed (10m max)", value: "wind_speed_10m_max" },
  { label: "Shortwave Radiation Sum", value: "shortwave_radiation_sum" }
];

const Dashboard: React.FC = () => {
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [selectedCity, setSelectedCity] = useState("");
  const [selectedYAxis, setSelectedYAxis] = useState("temperature");
  const [chartData, setChartData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const fetchChartData = async () => {
    console.log("Fetch data function");
  
    if (!selectedCity || !fromDate || !toDate) {
      console.log("Please fill in all the fields.");
      return;
    }
  
    try {
      setLoading(true);
      setError(""); // Clear previous errors
  
      // Request payload
      const requestPayload = {
        location: selectedCity,
        startDate: fromDate,
        endDate: toDate,
        parameters: [selectedYAxis], // Only one parameter is allowed at a time
      };
  
      console.log("Request Body:", JSON.stringify(requestPayload));
  
      // Send the request
      const response = await fetch("http://localhost:8080/api/Weather/processed", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestPayload),
      });
  
      // Log the full response to help diagnose the issue
      console.log("Response Status:", response.status);
      console.log("Response Headers:", response.headers);
  
      if (!response.ok) {
        throw new Error(`Failed to fetch data. Status: ${response.status}`);
      }
  
      const responseBody = await response.text(); // Get the raw response body
      console.log("Raw Response Body:", responseBody);
  
      try {
        const data = JSON.parse(responseBody); // Try to parse the response
        console.log("Parsed Data:", data);
  
        // Check if data and data.data exist
        if (!data || !data.daily || !data.daily.data) {
          throw new Error("Response data or data array is missing");
        }
  
        // Ensure the data is in the correct format for the chart (array of objects with 'name' and 'value')
        const processedData = data.daily.data.map((item: any) => ({
          name: item.xaxis, // Date 
          value: item.yaxis, // The corresponding data value
        }));
  
        console.log("Processed Data:", processedData);
        setChartData(processedData);
      } catch (e) {
        console.error("Error parsing JSON response:", e);
        setError("Error parsing response data.");
      }
    } catch (error) {
      console.error("Error fetching chart data:", error);
      setError("Error fetching chart data.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="dashboard-container">
      <h1 className="dashboard-title">Weather Dashboard</h1>

      <div className="input-row">
        <label htmlFor="location-search" className="input-label">Location</label>
        <select
          value={selectedCity}
          onChange={(e) => setSelectedCity(e.target.value)}
          className="city-dropdown"
        >
          <option value="">Select a city</option>
          {cities.map((city, index) => (
            <option key={index} value={city}>{city}</option>
          ))}
        </select>
      </div>

      <div className="input-row">
        <label className="input-label">Date Range</label>
        <div className="date-selectors">
          <div className="date-selector">
            <span>From: </span>
            <input type="date" className="date-input" 
              min="1950-01-01"
              max="2024-12-31"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)} />
          </div>
          <div className="date-selector">
            <span>To: </span>
            <input type="date" className="date-input" 
              min="1950-01-01"
              max="2024-12-31"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)} />
          </div>
        </div>
      </div>

      <div className="input-row">
        <label className="input-label">Y Axis</label>
        <select
          value={selectedYAxis}
          onChange={(e) => setSelectedYAxis(e.target.value)}
          className="y-axis-dropdown"
        >
          {yAxisOptions.map((option, index) => (
            <option key={index} value={option.value}>{option.label}</option>
          ))}
        </select>
      </div>

      <div className="search-button-container">
        <button 
          onClick={fetchChartData} 
          className="search-button" 
          style={{ border: "2px solid black", padding: "5px 10px", borderRadius: "5px" }}
        >
          Search
        </button>
      </div>

      {loading && <p>Loading...</p>}
      {error && <p style={{ color: "red" }}>{error}</p>}

      {chartData.length > 0 && (
        <div className="dashboard-card">
          <ResponsiveContainer width="100%" height={400}>
            <LineChart data={chartData} margin={{ top: 20, right: 60, bottom: 100, left: 40 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis 
                dataKey="name" 
                tickFormatter={(date: string) => {
                  const dateObj = new Date(date);
                  return dateObj.toLocaleDateString(); // Format date
                }} 
                angle={45} // Rotate the labels by 45 degrees
                textAnchor="start" // Align the text to the start of the rotated label
              />
              <YAxis 
                domain={['auto', 'auto']} // Dynamically scale Y-axis based on data
              />
              <Tooltip />
              <Line type="monotone" dataKey="value" stroke="#8884d8" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>


        </div>
      )}
    </div>
  );
};

export default Dashboard;
