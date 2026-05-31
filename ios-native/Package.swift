// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "VaultGuard",
    platforms: [.iOS(.v17)],
    dependencies: [
        .package(url: "https://github.com/groue/GRDB.swift", from: "6.27.0"),
        .package(url: "https://github.com/apple/swift-collections", from: "1.1.0"),
    ],
    targets: [
        .target(
            name: "VaultGuard",
            dependencies: [
                .product(name: "GRDB", package: "GRDB.swift"),
                .product(name: "Collections", package: "swift-collections"),
            ],
            path: "VaultGuard"
        ),
    ]
)
