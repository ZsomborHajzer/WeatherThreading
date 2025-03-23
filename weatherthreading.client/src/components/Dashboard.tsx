import React, { useState } from "react";
import { LineChart, XAxis, Line, Tooltip, CartesianGrid, ResponsiveContainer } from "recharts";
import "../index.css";

const data = [
  { name: "Jan", value: 30 },
  { name: "Feb", value: 50 },
  { name: "Mar", value: 40 },
  { name: "Apr", value: 70 },
  { name: "May", value: 60 },
];

const Dashboard: React.FC = () => {
  const [search, setSearch] = useState("");

  return (
<div className="dashboard-container">
  <h1 className="dashboard-title">Dashboard</h1>
  
  <div className="input-row">
    <label htmlFor="location-search" className="input-label">Location</label>
    <input
      id="location-search"
      type="text"
      placeholder="Search..."
      value={search}
      onChange={(e) => setSearch(e.target.value)}
      className="dashboard-search"
    />
  </div>

  <div className="input-row">
    <label className="input-label">X Axis</label>
    <div className="date-selectors">
      <div className="date-selector">
        <span>From: </span>
        <input type="date" className="date-input" />
      </div>
      <div className="date-selector">
        <span>To: </span>
        <input type="date" className="date-input" />
      </div>
    </div>
  </div>

  <div className="input-row">
    <label className="input-label">Y Axis</label>
    <select className="y-axis-dropdown">
      <option value="temperature">Temperature (2m)</option>
      <option value="precipitation">Precipitation</option>
      <option value="rain">Rain</option>
      <option value="snowfall">Snowfall</option>
      <option value="snowDepth">Snow Depth</option>
      <option value="relativeHumidity">Relative Humidity (2m)</option>
      <option value="windSpeed">Wind Speed (10m)</option>
    </select>
  </div>

  <div className="dashboard-card">
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={data}>
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
