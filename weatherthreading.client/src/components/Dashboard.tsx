import { LineChart, XAxis, Line, Tooltip, CartesianGrid, ResponsiveContainer } from "recharts";
import React, { useState } from "react";
import "../index.css";

const cities = [
  "Budapest, Hungary", "Bangkok, Thailand", "Zurich, Switzerland", "Los Angeles, USA", "Halifax, Canada", 
  "Rome, Italy", "London, UK", "Riga, Latvia", "Barcelona, Spain", "Shanghai, China", 
  "Tokyo, Japan", "Paris, France", "Stockholm, Sweden", "Munich, Germany", "Riyadh, Saudi Arabia"
];

const yAxisOptions = [
  { label: "Temperature", value: "temperature" },
  { label: "Relative Humidity", value: "relative_humidity" },
  { label: "Precipitation Sum", value: "precipitation_sum" },
  { label: "Precipitation Hours", value: "precipitation_hours" },
  { label: "Wind Speed", value: "wind_speed" },
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
    if (!selectedCity || !fromDate || !toDate) {
      console.log("Please fill in all the fields.");
      return;
    }
  
    try {
      setLoading(true);
      setError("");

      // Construct the request payload
      const requestPayload = {
        location: selectedCity,
        startDate: fromDate,
        endDate: toDate,
        parameters: [selectedYAxis], // Assuming only one parameter is selected at a time
      };

      const response = await fetch("/api/Weather/Processed", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(requestPayload),
      });

      if (!response.ok) {
        throw new Error("Failed to fetch data");
      }

      const data = await response.json();

      // Assuming the response is structured as { xaxistitle, yaxistitle, data }
      const processedData = data.data.map((item: any) => ({
        name: item.name, // Date (or other appropriate x-axis value)
        value: item.value, // The corresponding data value
      }));

      setChartData(processedData);
    } catch (error) {
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
              max={new Date().toISOString().split("T")[0]}
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)} />
          </div>
          <div className="date-selector">
            <span>To: </span>
            <input type="date" className="date-input" 
              min="1950-01-01"
              max={new Date().toISOString().split("T")[0]}
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
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" />
              <Tooltip />
              <Line type="monotone" dataKey="value" stroke="#8884d8" strokeWidth={2} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
};

export default Dashboard;
