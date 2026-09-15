# t-SNE 성능 개선 및 측정

## 적용 내용

### 동일 입력의 좌표 재사용

`TSNEProjectionModel`은 가장 최근에 성공한 분석 좌표 한 건을 메모리에 보관합니다. 정규화된 입력 행렬의 크기, 행·열 순서, 모든 double 값의 이진 표현, 실제 perplexity, random seed를 SHA-256으로 확인합니다.

입력이 같으면 PCA 초기화와 t-SNE 최적화를 건너뛰고 저장된 좌표를 복사해 반환합니다. 값·순서·설정이 바뀌면 다시 계산합니다. DraftNo, 결과 라벨, 상세 보고서는 현재 요청으로 구성하므로 라벨 변경도 반영됩니다.

캐시에는 작은 fingerprint와 2차원 좌표만 보관합니다. 고차원 입력 행렬을 추가로 장기 보관하지 않습니다. 프로세스를 종료하거나 다른 입력을 분석하면 이전 캐시를 사용할 수 없습니다. 동시에 요청한 서로 다른 분석은 캐시 잠금 밖에서 계산합니다.

이 기능은 동일 데이터의 **재분석·새로고침**에 효과가 있습니다. 현재 화면의 DraftNo 강조는 이미 기존 분석을 사용합니다.

### 첫 분석의 중복 처리 감소

- Exadata 분석에서 생성 직후 교체하던 중복 feature audit를 제거했습니다. 최종 보고서는 기존처럼 원본의 metadata, 비수치 항목, 누락 정보를 포함합니다.
- PCA 초기화에서 `Overwrite=false`로 생성되는 내부 작업 행렬을 사용해 불필요한 입력 복사 한 번을 제거했습니다.
- 로그의 행 수는 `Records.Count`로 읽습니다. 행 수만 확인하기 위한 전체 행·feature DataTable 생성을 제거했습니다.

## 계산 기준

Accord 3.8의 t-SNE 1,000회 반복, PCA 초기화, perplexity 보정, 수치 항목 선택, 평균 대체, 표준화, 좌표 방향 처리는 유지합니다.

기존 JSON 변환도 유지합니다. `FloatParseHandling.Decimal`을 사용하는 기존 변환을 생략하면 double 정밀도, 매우 작은 값, 숫자 범위 검증이 달라질 수 있습니다.

## 시간 로그 읽기

분석 후 기존 로그 파일에 `Performance` 항목이 기록됩니다.

`%LOCALAPPDATA%\SKhynix\TAS\TSNEScatter\AnalysisLogs\manual_tsne_latest_analysis.log`

| 항목 | 측정 범위 |
| --- | --- |
| Source preparation | 원본 실험 파싱, JSON 정규화, 파이프라인 입력 파싱 |
| Feature selection | 사용할 수치 항목 선택 및 행렬 생성 |
| Standardization | scaler 학습 및 표준화 |
| Projection | 입력 fingerprint, PCA 초기화, t-SNE 최적화, 좌표 복사 |
| KNN and verification | 차트 데이터 구성, KNN 준비, 수치 검증 |
| Feature audit | 항목별 상세 검사 보고서 생성 |
| Analysis total | 전체 분석 처리 및 결과 record 구성 |
| Projection cache | `Hit`: 좌표 재사용, `Miss`: 새 좌표 계산 |
| PCA initialization / t-SNE optimization | Projection 안에 포함된 세부 시간 |
| Chart binding | 차트 데이터 바인딩 호출 |
| Audit text generation | 상세 로그 문자열 생성 |

모든 시간은 밀리초입니다. 캐시 적중 시 PCA와 t-SNE 최적화 시간은 0이며, 실제 요청의 fingerprint·좌표 복사 시간은 Projection에 포함됩니다. Analysis total에는 데이터베이스 조회, 화면 바인딩·실제 화면 그리기, 로그 파일 쓰기가 포함되지 않습니다. 세부 단계 외의 결과 record 구성도 포함하므로 단계들의 합과 정확히 같지는 않습니다. 로그 생성과 파일 쓰기의 합은 Debug 출력에도 기록합니다.

## 실제 데이터에서 확인할 방법

1. 프로세스를 새로 시작하고 같은 PARAM_TYP의 데이터를 분석합니다. 분석 행 수, feature 수, `Miss` 상태와 단계별 시간을 기록합니다.
2. 입력과 설정을 유지한 채 다시 분석합니다. `Hit` 상태와 실제 총 소요 시간을 기록합니다.
3. 값 또는 분석 설정을 변경한 뒤 다시 분석해 필요한 경우 `Miss`로 바뀌는지 확인합니다.
4. 전후 좌표, 포함·제외 feature, 보고서, 검색 결과를 비교합니다.

사용자가 보고한 70초는 실제 데이터의 기존 관측값입니다. 합성 데이터의 검증 수치와 별도로, 위 로그를 사용해 실제 데이터에서 개선 폭을 측정해야 합니다.

## 합성 데이터 검증 결과

Windows의 Release 빌드에서 기존 HEAD와 수정 소스를 각각 분리 빌드했습니다. 실제 Accord 1,000회 최적화를 사용했으며, 작은 입력으로 런타임을 준비한 뒤 아래 입력을 처음 분석했습니다.

160건 × 80개 수치 항목, 단위 ms:

| 서비스 호출 | 변경 전 | 변경 후 |
| --- | ---: | ---: |
| 입력의 첫 AnalyzeSnapshot | 1,374.9 | 1,365.7 |
| 같은 입력의 첫 QueryDraftAsync | 1,327.7 | 62.7 |
| 같은 입력의 반복 QueryDraftAsync | 1,425.2 | 57.8 |

첫 분석의 차이는 측정 변동 수준입니다. 반복 분석 호출에서는 약 96% 감소했습니다. 이 표는 서비스가 전체 분석을 다시 요청하는 경우이며, 기존 화면의 TextBox 검색 성능을 뜻하지 않습니다. 화면 검색은 이미 현재 분석을 재사용하는 경로가 있습니다.

14개 비교 사례에서 포함·제외 항목, 정규화 값, scaler 통계, 좌표, KNN, 식별자·라벨, 상세 audit 및 내보낸 DataTable이 수치 허용 오차 `1e-12` 안에서 일치했습니다. 문자열과 구조는 정확히 비교했습니다. 누락·null·비수치·중첩 항목, 평균 대체, 대소문자, 소수점 변환, metadata 충돌 및 잘못된 입력을 포함합니다.

캐시 검증에서는 동일 입력 재사용, 값·perplexity·seed·행 순서 변경, 1,031개 항목으로 해시 버퍼를 넘는 입력, 반환 좌표 변경의 격리, 최신 식별자·라벨 반영을 확인했습니다. ReportMaker와 Pccb 호스트의 Release 빌드가 통과했습니다.

재실행 방법: [검증 도구 안내](../diagnostics/tsne-performance/README.md).
