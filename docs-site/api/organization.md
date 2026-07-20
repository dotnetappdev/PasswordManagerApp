# Collections, Categories & Tags

Three independent, orthogonal ways to organise items - a collection is a broad grouping, a category
narrows by kind, and tags are free-form labels. All three follow the same standard CRUD shape.

## Collections - `/api/collections`

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/collections` | All collections. |
| `GET` | `/api/collections/{id}` | A single collection. `404` if missing. |
| `POST` | `/api/collections` | Create. Body: `CreateCollectionDto` (`Name`, `Description`). Returns `201` with a `Location` header. |
| `PUT` | `/api/collections/{id}` | Update. Body: `UpdateCollectionDto`. |
| `DELETE` | `/api/collections/{id}` | Delete. `204` on success, `404` if missing. |

## Categories - `/api/categories`

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/categories` | All categories. |
| `GET` | `/api/categories/{id}` | A single category. |
| `GET` | `/api/categories/collection/{collectionId}` | Categories scoped to a collection. |
| `POST` | `/api/categories` | Create. Body: `CreateCategoryDto`. |
| `PUT` | `/api/categories/{id}` | Update. Body: `UpdateCategoryDto`. |
| `DELETE` | `/api/categories/{id}` | Delete. |

## Tags - `/api/tags`

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/tags` | All tags. |
| `GET` | `/api/tags/{id}` | A single tag. |
| `POST` | `/api/tags` | Create. Body: `CreateTagDto`. |
| `PUT` | `/api/tags/{id}` | Update. Body: `UpdateTagDto`. |
| `DELETE` | `/api/tags/{id}` | Delete. |

Password items reference these by ID (`CategoryId`, `CollectionId`, `TagIds[]`) - see
[Password Items](/api/password-items).
