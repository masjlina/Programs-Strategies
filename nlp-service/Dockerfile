FROM python:3.11-slim

WORKDIR /app

# Install FastAPI, Uvicorn, PyMorphy3, and the Ukrainian dictionaries
RUN pip install --no-cache-dir fastapi uvicorn pymorphy3 pymorphy3-dicts-uk pydantic

COPY . .

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
