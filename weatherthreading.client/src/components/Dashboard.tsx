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

  const fetchChartData = async () => {
    try {
      const response = await fetch(
        `/api/data?city=${encodeURIComponent(selectedCity)}&from=${fromDate}&to=${toDate}&yAxis=${selectedYAxis}`
      );
      
      if (!response.ok) {
        throw new Error(`Server error: ${response.status}`);
      }
  
      const result = await response.json();
      
      // Transform data to match Recharts expected format
      const formattedData = result.data.map((item: any) => ({
        name: item.xaxis,  // Will be used for X-axis
        value: item.yaxis  // Will be used for Y-axis
      }));
  
      setChartData(formattedData);
      
    } catch (error) {
      console.error("Fetch error:", error);
    
    }
  };

  
  return (
    <div className="dashboard-container">
      <h1 className="dashboard-title">Dashboard</h1>

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
        <label className="input-label">X Axis</label>
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
          id="y-axis-dropdown"
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
    </div>
  );
};

export default Dashboard;
