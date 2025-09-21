// MongoDB initialization script
db = db.getSiblingDB('AuditingDb');

// Create the audit entries collection with some indexes for performance
db.createCollection('AuditEntries');

// Create indexes for better query performance
db.AuditEntries.createIndex({ "Timestamp": -1 });
db.AuditEntries.createIndex({ "Method": 1 });
db.AuditEntries.createIndex({ "Path": 1 });
db.AuditEntries.createIndex({ "StatusCode": 1 });

print('MongoDB initialized successfully with AuditingDb database and indexes');
