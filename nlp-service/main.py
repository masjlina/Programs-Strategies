from fastapi import FastAPI
from pydantic import BaseModel
from typing import List
import pymorphy3

app = FastAPI(title="Ukrainian Lemmatizer Service")

# Initialize morph analyzer with Ukrainian dictionary
morph = pymorphy3.MorphAnalyzer(lang='uk')

class BatchRequest(BaseModel):
    words: List[str]

class BatchResponse(BaseModel):
    lemmas: List[str]

class SingleRequest(BaseModel):
    word: str

class SingleResponse(BaseModel):
    lemma: str

@app.post("/lemmatize", response_model=SingleResponse)
def lemmatize(request: SingleRequest):
    w = request.word.strip().lower()
    if not w:
        return SingleResponse(lemma="")
    parsed = morph.parse(w)
    lemma = parsed[0].normal_form if parsed else w
    return SingleResponse(lemma=lemma)

@app.post("/lemmatize-batch", response_model=BatchResponse)
def lemmatize_batch(request: BatchRequest):
    lemmas = []
    for word in request.words:
        w = word.strip().lower()
        if not w:
            lemmas.append("")
            continue
        parsed = morph.parse(w)
        lemma = parsed[0].normal_form if parsed else w
        lemmas.append(lemma)
    return BatchResponse(lemmas=lemmas)
