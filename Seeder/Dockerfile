FROM python:3.12-slim

WORKDIR /app

# Install dependencies
COPY Seeder/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy seeder script
#COPY Seeder/seeder.py .
COPY Seeder/determined_seeder.py .

# Healthcheck pour Docker Compose
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://server:3001/health || exit 1

# Run seeder
#CMD ["python", "seeder.py"]
CMD ["python", "determined_seeder.py"]