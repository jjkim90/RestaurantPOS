# RestaurantPOS

음식점을 위한 Windows 기반 POS(Point of Sale) 시스템

## 기술 스택
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Prism MVVM Framework
- Entity Framework Core 8.0
- SQL Server Express

## 프로젝트 설정

### 1. 사전 요구사항
- Visual Studio 2022
- .NET 8.0 SDK
- SQL Server Express

### 2. 데이터베이스 설정
1. SQL Server Express가 실행 중인지 확인
2. `RestaurantPOS.WPF` 폴더에서 `appsettings.template.json`을 `appsettings.json`으로 복사
3. 연결 문자열 수정 (필요한 경우)

### 3. 빌드 및 실행
```bash
# 솔루션 빌드
dotnet build

# 실행
dotnet run --project RestaurantPOS.WPF
```

첫 실행 시 데이터베이스가 자동으로 생성되고 초기 데이터가 입력됩니다.

## 주요 기능
- 테이블 관리 (공간별 테이블 배치)
- 메뉴 관리 (카테고리별 분류)
- 주문 처리
- 결제 관리

## 프로젝트 구조
```
RestaurantPOS/
├── RestaurantPOS.WPF/        # Presentation Layer
├── RestaurantPOS.Core/       # Domain Layer
├── RestaurantPOS.Services/   # Business Logic Layer
└── RestaurantPOS.Data/       # Data Access Layer
```

---
*Last updated: 2025-01*